    /**
     * <logo-animation> — SlideGenerator animated lockup
     *
     * Motion model:
     *   1. Icon starts centered inside the final lockup width.
     *   2. Icon moves left to its final slot.
     *   3. Name lives in its final clipped viewport, starts fully outside
     *      that viewport on the left, then translates into place.
     *
     * Proportions derived from Figma "App-Logo" frames (node 1:13 -> 1:39):
     *   icon 1018x1018 px, name 1634.83x1006.35 px -> name-to-icon ratio ~= 1.606.
     *
     * Attributes
     *   height      px height of the lockup (icon = square at this height). Default 32.
     *   delay       ms to hold icon-centered before animating.
     *   duration    ms for the split animation.
     *   gap         px between icon and name at rest. Default: height * 0.04.
     *   icon-src    path to app-icon.png. Default: same dir as this script.
     *   name-src    path to app-name.png. Default: same dir as this script.
     *   autoplay    set to "false" to skip auto-play (call .play() manually).
     *
     * Methods
     *   play()   Reset and replay the animation from the beginning.
     */
    (function () {
    const _base = (() => {
        const s = document.currentScript;
        return s ? s.src.replace(/[^/]*$/, "") : "";
    })();

    const NAME_W_RATIO = 1634.83 / 1018;
    const EASE = "cubic-bezier(0.15, 0.7, 0.1, 1)";
    const DEFAULT_DELAY = 1600;
    const DEFAULT_DURATION = 1100;

    class LogoAnimation extends HTMLElement {
        connectedCallback() {
        if (this._ready) return;
        this._ready = true;
        this._build();

        if (this.getAttribute("autoplay") !== "false") {
            const delay = parseInt(this.getAttribute("delay") ?? String(DEFAULT_DELAY), 10);
            this._timer = window.setTimeout(() => this._run(), delay);
        }
        }

        disconnectedCallback() {
        this._cancel();
        }

        _build() {
        const h = parseInt(this.getAttribute("height") ?? "32", 10);
        const gap = parseInt(this.getAttribute("gap") ?? String(Math.round(h * 0.04)), 10);
        const iconSrc = this.getAttribute("icon-src") || _base + "app-icon.png";
        const nameSrc = this.getAttribute("name-src") || _base + "app-name.png";
        const nameW = Math.round(h * NAME_W_RATIO);
        const totalW = h + gap + nameW;
        const iconStartX = Math.round((totalW - h) / 2);
        const nameEndX = h + gap;
        const nameHiddenX = -nameW;

        this._metrics = {
            h,
            gap,
            nameW,
            totalW,
            iconStartX,
            nameEndX,
            nameHiddenX,
        };

        Object.assign(this.style, {
            display: "inline-block",
            position: "relative",
            overflow: "hidden",
            width: totalW + "px",
            height: h + "px",
            flexShrink: "0",
            verticalAlign: "middle",
            contain: "layout paint",
        });

        const icon = document.createElement("img");
        icon.src = iconSrc;
        icon.alt = "SlideGenerator";
        icon.width = h;
        icon.height = h;
        Object.assign(icon.style, {
            position: "absolute",
            left: "0",
            top: "0",
            width: h + "px",
            height: h + "px",
            objectFit: "contain",
            transform: `translate3d(${iconStartX}px, 0, 0)`,
            zIndex: "2",
            willChange: "transform",
            userSelect: "none",
        });

        const nameViewport = document.createElement("div");
        Object.assign(nameViewport.style, {
            position: "absolute",
            left: nameEndX + "px",
            top: "0",
            width: nameW + "px",
            height: h + "px",
            overflow: "hidden",
            zIndex: "1",
            pointerEvents: "none",
        });

        const name = document.createElement("img");
        name.src = nameSrc;
        name.alt = "";
        name.setAttribute("aria-hidden", "true");
        name.width = nameW;
        name.height = h;
        Object.assign(name.style, {
            position: "absolute",
            left: "0",
            top: "0",
            width: nameW + "px",
            height: h + "px",
            objectFit: "contain",
            objectPosition: "left center",
            opacity: "1",
            visibility: "visible",
            transform: `translate3d(${nameHiddenX}px, 0, 0)`,
            willChange: "transform",
            userSelect: "none",
        });

        nameViewport.appendChild(name);
        this.replaceChildren(nameViewport, icon);
        this._icon = icon;
        this._name = name;
        this._nameViewport = nameViewport;
        }

        _run() {
        this._cancel();

        const { iconStartX, nameHiddenX } = this._metrics;
        const dur = parseInt(this.getAttribute("duration") ?? String(DEFAULT_DURATION), 10);
        const reduceMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches;

        if (reduceMotion) {
            this._setFinal();
            return;
        }

        this._icon.style.transform = `translate3d(${iconStartX}px, 0, 0)`;
        this._name.style.opacity = "1";
        this._name.style.visibility = "visible";
        this._name.style.transform = `translate3d(${nameHiddenX}px, 0, 0)`;

        const iconAnim = this._icon.animate(
            [
            { transform: `translate3d(${iconStartX}px, 0, 0)` },
            { transform: "translate3d(0, 0, 0)" },
            ],
            {
            duration: dur,
            easing: EASE,
            fill: "forwards",
            }
        );

        const nameAnim = this._name.animate(
            [
            { transform: `translate3d(${nameHiddenX}px, 0, 0)` },
            { transform: "translate3d(0, 0, 0)" },
            ],
            {
            duration: dur,
            easing: EASE,
            fill: "forwards",
            }
        );

        this._animations = [iconAnim, nameAnim];
        Promise.allSettled(this._animations.map((animation) => animation.finished)).then(() => {
            if (this._animations?.includes(iconAnim)) {
            this._setFinal();
            this._animations = [];
            }
        });
        }

        _setFinal() {
        this._icon.style.transform = "translate3d(0, 0, 0)";
        this._name.style.opacity = "1";
        this._name.style.visibility = "visible";
        this._name.style.transform = "translate3d(0, 0, 0)";
        }

        _cancel() {
        window.clearTimeout(this._timer);
        this._animations?.forEach((animation) => animation.cancel());
        this._animations = [];
        }

        play() {
        const delay = parseInt(this.getAttribute("delay") ?? String(DEFAULT_DELAY), 10);
        const { iconStartX, nameHiddenX } = this._metrics;

        this._cancel();
        this._icon.style.transform = `translate3d(${iconStartX}px, 0, 0)`;
        this._name.style.opacity = "1";
        this._name.style.visibility = "visible";
        this._name.style.transform = `translate3d(${nameHiddenX}px, 0, 0)`;
        this._timer = window.setTimeout(() => this._run(), delay);
        }
    }

    if (!customElements.get("logo-animation")) {
        customElements.define("logo-animation", LogoAnimation);
    }
    })();
