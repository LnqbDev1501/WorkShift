/**
 * Page Mascot - Interactive Sprite Mascot for Admin Interface
 * Lightweight Vanilla JS implementation (Canvas-based)
 */
(function () {
    class PageMascot {
        constructor(options = {}) {
            this.canvas = typeof options.target === 'string' 
                ? document.querySelector(options.target) 
                : options.target;
            
            if (!this.canvas) return;

            this.ctx = this.canvas.getContext('2d');
            this.directionsSrc = options.directions || '/mascots/fox-directions.webp';
            this.reactionsSrc = options.reactions || '/mascots/fox-reactions.webp';
            this.size = options.size || 64; // Internal resolution
            this.deadzone = options.deadzone || 24; // Distance in px where mascot looks straight

            this.directionsImg = new Image();
            this.reactionsImg = new Image();
            this.isReady = false;

            this.currentMode = 'directions'; // 'directions' or 'reaction'
            this.currentCol = 1; // 0..2
            this.currentRow = 1; // 0..2
            this.reactionFrame = 0;
            this.isReacting = false;
            this.reactionTimer = null;

            this.init();
        }

        init() {
            // Set canvas internal resolution for crisp rendering (retina ready)
            this.canvas.width = this.size;
            this.canvas.height = this.size;

            let loadedCount = 0;
            const checkLoaded = () => {
                loadedCount++;
                if (loadedCount === 2) {
                    this.isReady = true;
                    this.draw();
                    this.bindEvents();
                    this.startIdleBlinking();
                }
            };

            this.directionsImg.onload = checkLoaded;
            this.reactionsImg.onload = checkLoaded;

            this.directionsImg.src = this.directionsSrc;
            this.reactionsImg.src = this.reactionsSrc;
        }

        draw() {
            if (!this.isReady) return;

            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

            const activeImg = (this.currentMode === 'reaction') 
                ? this.reactionsImg 
                : this.directionsImg;

            const frameWidth = activeImg.naturalWidth / 3;
            const frameHeight = activeImg.naturalHeight / 3;

            let col = this.currentCol;
            let row = this.currentRow;

            if (this.currentMode === 'reaction') {
                col = this.reactionFrame % 3;
                row = Math.floor(this.reactionFrame / 3);
            }

            const sx = col * frameWidth;
            const sy = row * frameHeight;

            this.ctx.imageSmoothingEnabled = true;
            this.ctx.imageSmoothingQuality = 'high';
            this.ctx.drawImage(
                activeImg,
                sx, sy, frameWidth, frameHeight,
                0, 0, this.canvas.width, this.canvas.height
            );
        }

        updateDirection(mouseX, mouseY) {
            if (this.isReacting) return;

            const rect = this.canvas.getBoundingClientRect();
            const cx = rect.left + rect.width / 2;
            const cy = rect.top + rect.height / 2;

            const dx = mouseX - cx;
            const dy = mouseY - cy;
            const distance = Math.hypot(dx, dy);

            let newCol = 1;
            let newRow = 1;

            if (distance > this.deadzone) {
                // Calculate angle in degrees [-180, 180]
                const deg = Math.atan2(dy, dx) * (180 / Math.PI);

                if (deg >= -157.5 && deg < -112.5) {
                    // Top-Left
                    newCol = 0; newRow = 0;
                } else if (deg >= -112.5 && deg < -67.5) {
                    // Top
                    newCol = 1; newRow = 0;
                } else if (deg >= -67.5 && deg < -22.5) {
                    // Top-Right
                    newCol = 2; newRow = 0;
                } else if (deg >= -22.5 && deg < 22.5) {
                    // Right
                    newCol = 2; newRow = 1;
                } else if (deg >= 22.5 && deg < 67.5) {
                    // Bottom-Right
                    newCol = 2; newRow = 2;
                } else if (deg >= 67.5 && deg < 112.5) {
                    // Bottom
                    newCol = 1; newRow = 2;
                } else if (deg >= 112.5 && deg < 157.5) {
                    // Bottom-Left
                    newCol = 0; newRow = 2;
                } else {
                    // Left
                    newCol = 0; newRow = 1;
                }
            }

            if (newCol !== this.currentCol || newRow !== this.currentRow) {
                this.currentCol = newCol;
                this.currentRow = newRow;
                this.currentMode = 'directions';
                this.draw();
            }
        }

        triggerReaction(frameCount = 9, fps = 14) {
            if (this.isReacting) return;
            this.isReacting = true;
            this.currentMode = 'reaction';
            this.reactionFrame = 0;

            const frameInterval = 1000 / fps;
            const totalFrames = Math.min(frameCount, 9);

            const animate = () => {
                this.draw();
                this.reactionFrame++;
                if (this.reactionFrame < totalFrames) {
                    this.reactionTimer = setTimeout(animate, frameInterval);
                } else {
                    this.isReacting = false;
                    this.currentMode = 'directions';
                    this.draw();
                }
            };

            animate();
        }

        startIdleBlinking() {
            const queueNextBlink = () => {
                const delay = 3500 + Math.random() * 4000;
                setTimeout(() => {
                    if (!this.isReacting && this.isReady) {
                        // Quick blink: play first 3 frames or subtle reaction
                        this.triggerReaction(4, 12);
                    }
                    queueNextBlink();
                }, delay);
            };
            queueNextBlink();
        }

        bindEvents() {
            // Track cursor movement across window
            window.addEventListener('mousemove', (e) => {
                this.updateDirection(e.clientX, e.clientY);
            }, { passive: true });

            // Click / poke reaction
            this.canvas.parentElement?.addEventListener('click', (e) => {
                e.stopPropagation();
                // Add a cute squish animation to the container
                const parent = this.canvas.parentElement;
                parent.style.transform = 'scale(0.88)';
                setTimeout(() => {
                    parent.style.transform = '';
                }, 150);

                this.triggerReaction(9, 16);
            });
        }
    }

    // Auto-initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', () => {
        const mascotEl = document.querySelector('#sidebar-mascot');
        if (mascotEl) {
            window.sidebarMascot = new PageMascot({
                target: mascotEl,
                directions: '/mascots/fox-directions.webp',
                reactions: '/mascots/fox-reactions.webp',
                size: 96
            });
        }
    });

    window.PageMascot = PageMascot;
})();
