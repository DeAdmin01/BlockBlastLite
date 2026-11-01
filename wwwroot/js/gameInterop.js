// Interaction, Drag & Drop Engine and Dynamic Theme Interop for Block Blast Lite

window.gameInterop = {
    dotNetHelper: null,
    activeDragPieceId: null,
    isDragging: false,
    grabOffsetX: 0,
    grabOffsetY: 0,
    pieceRows: 1,
    pieceCols: 1,
    currentHoverCell: null,
    fingerOffsetY: 125, // Authentic Block Blast vertical offset: piece floats well above the finger

    setFingerOffsetY: function (offset) {
        const val = Number(offset);
        this.fingerOffsetY = isNaN(val) ? 125 : Math.max(0, val);
    },

    registerHelper: function (helper) {
        this.dotNetHelper = helper;
    },

    setSystemPalette: function (palette) {
        if (!palette) return;
        const root = document.documentElement;
        try {
            if (palette.accentPrimary) {
                root.style.setProperty('--color-accent', palette.accentPrimary);
                root.style.setProperty('--color-accent-glow', palette.accentPrimary + '4D');
            }
            if (palette.accentContainer) {
                root.style.setProperty('--color-accent-container', palette.accentContainer);
            }
            if (palette.accentSubtle) {
                root.style.setProperty('--color-accent-subtle', palette.accentSubtle);
            }
            if (palette.background) {
                root.style.setProperty('--dynamic-bg', palette.background);
            }
            if (palette.surface) {
                root.style.setProperty('--dynamic-surface', palette.surface);
            }
            if (palette.surfaceVariant) {
                root.style.setProperty('--dynamic-surface-variant', palette.surfaceVariant);
            }
        } catch (e) {}
    },

    applyThemeMode: function (mode) {
        const root = document.documentElement;
        const body = document.body;

        root.setAttribute('data-theme', mode);
        if (body) {
            body.setAttribute('data-theme', mode);
            body.classList.remove('theme-light', 'theme-dark');
            if (mode === 'light') {
                body.classList.add('theme-light');
                root.classList.add('theme-light');
                root.classList.remove('theme-dark');
            } else if (mode === 'dark') {
                body.classList.add('theme-dark');
                root.classList.add('theme-dark');
                root.classList.remove('theme-light');
            } else {
                root.classList.remove('theme-light', 'theme-dark');
            }
        }
    },

    applySkin: function (skin, isAnimated = false) {
        const root = document.documentElement;
        root.setAttribute('data-skin', skin);
        if (document.body) {
            document.body.setAttribute('data-skin', skin);
        }
        const appContainer = document.querySelector('.game-app-container');
        if (appContainer) {
            appContainer.setAttribute('data-skin', skin);
            // Dynamically strip any previous skin- classes
            appContainer.className = appContainer.className.replace(/\bskin-[a-z0-9_-]+\b/g, '').trim();
            appContainer.classList.add('skin-' + skin);

            if (isAnimated) {
                appContainer.classList.remove('skin-shifting');
                void appContainer.offsetWidth; // Trigger reflow for smooth animation restart
                appContainer.classList.add('skin-shifting');
                setTimeout(() => appContainer.classList.remove('skin-shifting'), 650);
            }
        }
    },

    boardMetrics: null,
    rafId: null,
    pendingClientX: 0,
    pendingClientY: 0,
    ghostEl: null,

    // Calculate grid cell under the dragged piece using cached board metrics (Zero reflows / 0ms cost)
    getCellFromPiecePosition: function (pieceLeft, pieceTop) {
        if (!this.boardMetrics || !this.boardMetrics.cellSize) return null;

        const col = Math.round((pieceLeft - this.boardMetrics.innerLeft) / this.boardMetrics.cellSize);
        const row = Math.round((pieceTop - this.boardMetrics.innerTop) / this.boardMetrics.cellSize);

        if (row >= 0 && row < 8 && col >= 0 && col < 8) {
            return { row, col };
        }
        return null;
    },

    startDragFromElement: function (slotId, pieceId, rows, cols, clientX, clientY) {
        this.activeDragPieceId = pieceId;
        this.pieceRows = rows;
        this.pieceCols = cols;
        this.isDragging = true;
        this.currentHoverCell = null;

        if (window.blockBlastAudio) {
            window.blockBlastAudio.playPickup();
        }

        // Cache board metrics once at gesture start
        const board = document.getElementById('game-board');
        let boardCellSize = 42;
        if (board) {
            const bRect = board.getBoundingClientRect();
            boardCellSize = (bRect.width - 16) / 8;
            this.boardMetrics = {
                innerLeft: bRect.left + 8,
                innerTop: bRect.top + 8,
                cellSize: boardCellSize
            };
        }

        const slotElement = document.getElementById(slotId);
        this.ghostEl = document.getElementById('drag-floating-ghost');

        if (slotElement && this.ghostEl) {
            this.ghostEl.innerHTML = '';
            const polyomino = slotElement.querySelector('.polyomino-container');
            if (polyomino) {
                const polyRect = polyomino.getBoundingClientRect();
                
                // Record the exact grab offset within the piece
                this.grabOffsetX = clientX - polyRect.left;
                this.grabOffsetY = clientY - polyRect.top;

                // Safety fallback if grab was slightly outside
                if (this.grabOffsetX < 0 || this.grabOffsetX > polyRect.width) {
                    this.grabOffsetX = polyRect.width / 2;
                }
                if (this.grabOffsetY < 0 || this.grabOffsetY > polyRect.height) {
                    this.grabOffsetY = polyRect.height / 2;
                }

                const clone = polyomino.cloneNode(true);
                clone.classList.add('in-drag');
                clone.style.setProperty('--drag-cell-size', `${boardCellSize}px`);
                this.ghostEl.appendChild(clone);

                // Adjust grab offset proportionally if the cloned piece is scaled up to board cell size
                const originalCellSize = polyRect.width / cols;
                if (originalCellSize > 0) {
                    const scaleFactor = boardCellSize / originalCellSize;
                    this.grabOffsetX *= scaleFactor;
                    this.grabOffsetY *= scaleFactor;
                }
            }
        }

        this.pendingClientX = clientX;
        this.pendingClientY = clientY;
        this.updateDragPosition(clientX, clientY);

        const moveHandler = (e) => {
            if (!this.isDragging) return;
            const touch = e.touches ? e.touches[0] : e;
            this.pendingClientX = touch.clientX;
            this.pendingClientY = touch.clientY;

            // Decouple touch rate (240Hz+) from display refresh (120Hz) via requestAnimationFrame
            if (!this.rafId) {
                this.rafId = requestAnimationFrame(() => {
                    this.rafId = null;
                    if (!this.isDragging || !this.activeDragPieceId) return;
                    this.handleDragFrame();
                });
            }
        };

        const endHandler = (e) => {
            if (!this.isDragging) return;
            window.removeEventListener('pointermove', moveHandler);
            window.removeEventListener('pointerup', endHandler);
            window.removeEventListener('pointercancel', endHandler);
            window.removeEventListener('touchmove', moveHandler);
            window.removeEventListener('touchend', endHandler);
            window.removeEventListener('touchcancel', endHandler);

            if (this.rafId) {
                cancelAnimationFrame(this.rafId);
                this.rafId = null;
            }

            const touch = e.changedTouches ? e.changedTouches[0] : (e.clientX != null ? e : { clientX: this.pendingClientX, clientY: this.pendingClientY });
            this.handleDragEnd(touch.clientX, touch.clientY);
        };

        if (window.PointerEvent) {
            window.addEventListener('pointermove', moveHandler, { passive: true });
            window.addEventListener('pointerup', endHandler);
            window.addEventListener('pointercancel', endHandler);
        } else {
            window.addEventListener('touchmove', moveHandler, { passive: true });
            window.addEventListener('touchend', endHandler);
            window.addEventListener('touchcancel', endHandler);
        }
    },

    handleDragFrame: function () {
        const clientX = this.pendingClientX;
        const clientY = this.pendingClientY;

        this.updateDragPosition(clientX, clientY);

        // Compute piece top-left position floating above finger
        const pieceLeft = clientX - this.grabOffsetX;
        const pieceTop = clientY - this.grabOffsetY - this.fingerOffsetY;

        const cell = this.getCellFromPiecePosition(pieceLeft, pieceTop);
        if (this.dotNetHelper) {
            if (cell) {
                if (!this.currentHoverCell || this.currentHoverCell.row !== cell.row || this.currentHoverCell.col !== cell.col) {
                    this.currentHoverCell = cell;
                    this.dotNetHelper.invokeMethodAsync('OnDragHoverCell', this.activeDragPieceId, cell.row, cell.col);
                }
            } else if (this.currentHoverCell !== null) {
                this.currentHoverCell = null;
                this.dotNetHelper.invokeMethodAsync('OnDragLeaveBoard');
            }
        }
    },

    handleDragEnd: function (clientX, clientY) {
        if (!this.isDragging || !this.activeDragPieceId) return;

        const pieceId = this.activeDragPieceId;
        this.isDragging = false;
        this.activeDragPieceId = null;
        this.currentHoverCell = null;

        const pieceLeft = clientX - this.grabOffsetX;
        const pieceTop = clientY - this.grabOffsetY - this.fingerOffsetY;
        const cell = this.getCellFromPiecePosition(pieceLeft, pieceTop);

        if (this.dotNetHelper) {
            if (cell) {
                this.dotNetHelper.invokeMethodAsync('OnDropPieceOnCell', pieceId, cell.row, cell.col);
            } else {
                this.dotNetHelper.invokeMethodAsync('OnCancelDrag');
            }
        }

        this.hideDragGhost();
    },

    updateDragPosition: function (clientX, clientY) {
        if (!this.ghostEl) {
            this.ghostEl = document.getElementById('drag-floating-ghost');
        }
        if (!this.ghostEl) return;

        const pieceLeft = clientX - this.grabOffsetX;
        const pieceTop = clientY - this.grabOffsetY - this.fingerOffsetY;

        this.ghostEl.style.display = 'block';
        this.ghostEl.style.transform = `translate3d(${pieceLeft}px, ${pieceTop}px, 0)`;
    },

    hideDragGhost: function () {
        if (this.rafId) {
            cancelAnimationFrame(this.rafId);
            this.rafId = null;
        }
        this.boardMetrics = null;
        if (!this.ghostEl) {
            this.ghostEl = document.getElementById('drag-floating-ghost');
        }
        if (this.ghostEl) {
            this.ghostEl.style.display = 'none';
            this.ghostEl.innerHTML = '';
        }
    },

    vibrateHaptic: function (durationMs = 25) {
        if (navigator.vibrate) {
            try {
                navigator.vibrate(durationMs);
            } catch (e) {}
        }
    },

    // Guaranteed 3.5-Second Preloader Controller with Rich Micro-Animations
    preloaderStartTime: Date.now(),
    preloaderMinDuration: 3500,
    preloaderProgress: 0,
    isPreloaderDismissRequested: false,
    preloaderIntervalId: null,

    initPreloader: function () {
        const fill = document.getElementById('preloader-fill');
        const percent = document.getElementById('preloader-percent');
        const status = document.getElementById('preloader-status');
        const overlay = document.getElementById('app-preloader');
        if (!overlay) return;

        const phrases = [
            { at: 0, text: 'Загрузка 50 сочных тем...' },
            { at: 28, text: 'Подготовка игрового поля...' },
            { at: 58, text: 'Настройка тактильного отклика...' },
            { at: 85, text: 'Запуск фоновой музыки...' },
            { at: 96, text: 'Готово к игре!' }
        ];

        const duration = this.preloaderMinDuration;
        const start = this.preloaderStartTime;

        const updateProgress = () => {
            const elapsed = Date.now() - start;
            let p = Math.min(100, Math.floor((elapsed / duration) * 100));

            // If dismiss hasn't been requested yet by Blazor, hold at 98%
            if (!this.isPreloaderDismissRequested && p >= 98) {
                p = 98;
            }

            this.preloaderProgress = p;

            if (fill) fill.style.width = p + '%';
            if (percent) percent.textContent = p + '%';

            if (status) {
                for (let i = phrases.length - 1; i >= 0; i--) {
                    if (p >= phrases[i].at) {
                        if (status.textContent !== phrases[i].text) {
                            status.textContent = phrases[i].text;
                        }
                        break;
                    }
                }
            }

            if (p >= 100 && this.isPreloaderDismissRequested) {
                if (this.preloaderIntervalId) {
                    cancelAnimationFrame(this.preloaderIntervalId);
                    this.preloaderIntervalId = null;
                }
                this.finishPreloaderDismiss(overlay);
            } else {
                this.preloaderIntervalId = requestAnimationFrame(updateProgress);
            }
        };

        this.preloaderIntervalId = requestAnimationFrame(updateProgress);
    },

    dismissPreloader: function () {
        this.isPreloaderDismissRequested = true;
        const overlay = document.getElementById('app-preloader');
        if (!overlay) return;

        const elapsed = Date.now() - this.preloaderStartTime;
        if (elapsed >= this.preloaderMinDuration) {
            const fill = document.getElementById('preloader-fill');
            const percent = document.getElementById('preloader-percent');
            const status = document.getElementById('preloader-status');
            if (fill) fill.style.width = '100%';
            if (percent) percent.textContent = '100%';
            if (status) status.textContent = 'Готово к игре!';
            setTimeout(() => this.finishPreloaderDismiss(overlay), 120);
        }
    },

    finishPreloaderDismiss: function (overlay) {
        if (!overlay || overlay.classList.contains('dismissed')) return;
        overlay.classList.add('dismissed');
        setTimeout(() => {
            try { overlay.remove(); } catch (e) {}
        }, 650);
    }
};

// Auto-start preloader animation right away
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => window.gameInterop.initPreloader());
} else {
    window.gameInterop.initPreloader();
}
