/* =========================================================================
   BMW Full-Page Scrollytelling — Optimized Performance Engine
   ========================================================================= */

document.addEventListener("DOMContentLoaded", () => {
  // ── Shared State ──────────────────────────────────────────────────────
  const isMobile = window.innerWidth <= 768 || "ontouchstart" in window;
  let mouseX = 0,
    mouseY = 0,
    glowX = 0,
    glowY = 0;
  let targetProgress = 0,
    currentProgress = 0;
  let ready = false;
  let canvasVisible = true;
  let statsVisible = false;
  let twVisible = false;
  let twStarted = false;

  // ── Cache DOM refs once ───────────────────────────────────────────────
  const arrow = document.getElementById("cursor-arrow");
  const glow = document.getElementById("cursor-glow");
  const canvas = document.getElementById("bmw-canvas");
  const ctx = canvas ? canvas.getContext("2d", { alpha: false }) : null;
  const scrollContainer = document.getElementById("scroll-container");
  const loadingScreen = document.getElementById("loading-screen");
  const loaderPercent = document.getElementById("loader-percent");
  const canvasWrap = document.getElementById("canvas-wrap");
  const floatingNav = document.getElementById("floating-nav");
  const overlayHero = document.getElementById("overlay-hero");
  const overlayPerf = document.getElementById("overlay-perf");
  const overlayDesign = document.getElementById("overlay-design");
  const overlaySpecs = document.getElementById("overlay-specs");
  const overlayCta = document.getElementById("overlay-cta");
  const statsSection = document.getElementById("stats-section");
  const statCards = document.querySelectorAll(".stat-card");
  const statNumbers = document.querySelectorAll(".stat-number");
  const testimonialsSection = document.getElementById("testimonials");
  const testimonialCards = document.querySelectorAll(".testimonial-card");
  const twLine1 = document.getElementById("tw-line1");
  const twLine2 = document.getElementById("tw-line2");
  const twSection = document.getElementById("typewriter-section");
  const textarea = document.querySelector(".form-textarea");
  const charCount = document.querySelector(".char-count");
  const glassForm = document.getElementById("glass-form");

  // ── Custom Cursor (desktop only) ───────────────────────────────────
  if (!isMobile) {
    document.addEventListener(
      "mousemove",
      (e) => {
        mouseX = e.clientX;
        mouseY = e.clientY;
        if (arrow)
          arrow.style.transform = `translate3d(${mouseX}px,${mouseY}px,0)`;
      },
      { passive: true },
    );

    document
      .querySelectorAll(
        "a, button, .stat-card, .testimonial-card, .nav-cta-btn, .form-input, .form-textarea, .form-submit",
      )
      .forEach((el) => {
        el.addEventListener(
          "mouseenter",
          () => glow && glow.classList.add("hover"),
        );
        el.addEventListener(
          "mouseleave",
          () => glow && glow.classList.remove("hover"),
        );
      });
    document.addEventListener(
      "mousedown",
      () => glow && glow.classList.add("click"),
    );
    document.addEventListener(
      "mouseup",
      () => glow && glow.classList.remove("click"),
    );

    document.body.style.cursor = "none";
    document
      .querySelectorAll("a, button, input, textarea")
      .forEach((el) => (el.style.cursor = "none"));
  } else {
    // Hide custom cursor elements on mobile
    if (arrow) arrow.style.display = "none";
    if (glow) glow.style.display = "none";
  }

  // ── Testimonial Cards (one-shot, no loop) ─────────────────────────────
  if (testimonialsSection) {
    let tDone = false;
    new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && !tDone) {
          tDone = true;
          testimonialCards.forEach((c, i) =>
            setTimeout(() => c.classList.add("visible"), i * 250),
          );
        }
      },
      { threshold: 0.2 },
    ).observe(testimonialsSection);
  }

  // ── Typewriter (pauses when off-screen) ───────────────────────────────
  const LINE1 = "Ready to rent your dream car?";
  const LINE2 =
    "Discover our premium vehicle collection at RentWheels — find the perfect car for your journey.";
  let twIntervalIds = [];

  const clearTwIntervals = () => {
    twIntervalIds.forEach((id) => clearInterval(id));
    twIntervalIds = [];
  };

  const typeText = (element, text, speed, callback) => {
    let i = 0;
    const cursor = document.createElement("span");
    cursor.className = "tw-cursor";
    element.appendChild(cursor);
    const id = setInterval(() => {
      element.insertBefore(document.createTextNode(text[i]), cursor);
      i++;
      if (i >= text.length) {
        clearInterval(id);
        if (callback) setTimeout(callback, 300);
      }
    }, speed);
    twIntervalIds.push(id);
  };

  const eraseText = (element, speed, callback) => {
    const id = setInterval(() => {
      const textNodes = Array.from(element.childNodes).filter(
        (n) => n.nodeType === 3,
      );
      if (textNodes.length === 0) {
        clearInterval(id);
        if (callback) setTimeout(callback, 400);
        return;
      }
      const last = textNodes[textNodes.length - 1];
      if (last.textContent.length > 1)
        last.textContent = last.textContent.slice(0, -1);
      else last.remove();
    }, speed);
    twIntervalIds.push(id);
  };

  const runTypewriterLoop = () => {
    if (!twVisible) return; // Stop if scrolled away
    twLine1.textContent = "";
    twLine2.textContent = "";
    typeText(twLine1, LINE1, 35, () => {
      if (!twVisible) return;
      const c1 = twLine1.querySelector(".tw-cursor");
      if (c1) c1.remove();
      typeText(twLine2, LINE2, 20, () => {
        setTimeout(() => {
          if (!twVisible) return;
          const c2 = twLine2.querySelector(".tw-cursor");
          if (c2) c2.remove();
          eraseText(twLine2, 10, () => {
            eraseText(twLine1, 15, () => {
              setTimeout(runTypewriterLoop, 800);
            });
          });
        }, 3000);
      });
    });
  };

  if (twSection) {
    new IntersectionObserver(
      (entries) => {
        twVisible = entries[0].isIntersecting;
        if (twVisible && !twStarted) {
          twStarted = true;
          runTypewriterLoop();
        }
      },
      { threshold: 0.3 },
    ).observe(twSection);
  }

  // ── Scroll-Triggered Animations (one-shot) ────────────────────────────
  const animEls = document.querySelectorAll(".anim, .anim-stagger");
  if (animEls.length) {
    const ao = new IntersectionObserver(
      (entries) => {
        entries.forEach((e) => {
          if (e.isIntersecting) {
            e.target.classList.add("visible");
            ao.unobserve(e.target);
          }
        });
      },
      { threshold: 0.15, rootMargin: "0px 0px -40px 0px" },
    );
    animEls.forEach((el) => ao.observe(el));
  }

  // ── Contact Form ──────────────────────────────────────────────────────
  if (textarea && charCount) {
    textarea.addEventListener("input", () => {
      charCount.textContent = `${300 - textarea.value.length}/300`;
    });
  }
  if (glassForm) {
    glassForm.addEventListener("submit", (e) => {
      e.preventDefault();
      const btn = glassForm.querySelector(".form-submit");
      btn.textContent = "✓ Sent!";
      btn.style.background = "linear-gradient(135deg, #22c55e, #16a34a)";
      setTimeout(() => {
        btn.innerHTML = 'Submit Form <span class="submit-arrow">→</span>';
        btn.style.background = "";
        glassForm.reset();
        if (charCount) charCount.textContent = "300/300";
      }, 2500);
    });
  }

  // ── Stats: Count only when visible, pause when off-screen ─────────────
  let countAnimIds = [];

  const animateCount = (el) => {
    const target = parseFloat(el.dataset.target);
    const suffix = el.dataset.suffix || "";
    const decimals = parseInt(el.dataset.decimals) || 0;
    const duration = 2000;

    const runCount = () => {
      if (!statsVisible) return; // Pause when off-screen
      const start = performance.now();
      const tick = (now) => {
        if (!statsVisible) return; // Stop ticking if hidden
        const progress = Math.min((now - start) / duration, 1);
        const eased = 1 - Math.pow(1 - progress, 3);
        el.textContent = (eased * target).toFixed(decimals) + suffix;
        if (progress < 1) {
          const id = requestAnimationFrame(tick);
          countAnimIds.push(id);
        } else {
          setTimeout(() => {
            if (!statsVisible) return;
            el.textContent = "0" + suffix;
            setTimeout(runCount, 400);
          }, 3000);
        }
      };
      const id = requestAnimationFrame(tick);
      countAnimIds.push(id);
    };
    runCount();
  };

  if (statsSection) {
    let statsTriggered = false;
    new IntersectionObserver(
      (entries) => {
        statsVisible = entries[0].isIntersecting;
        if (statsVisible && !statsTriggered) {
          statsTriggered = true;
          statCards.forEach((c, i) =>
            setTimeout(() => c.classList.add("visible"), i * 200),
          );
          statNumbers.forEach((n, i) =>
            setTimeout(() => animateCount(n), i * 200 + 400),
          );
        }
      },
      { threshold: 0.15 },
    ).observe(statsSection);
  }

  // ── Canvas Setup ──────────────────────────────────────────────────────
  if (!canvas || !ctx) return;

  const TOTAL_FRAMES = 82;
  const EASE = isMobile ? 0.08 : 0.04; // Faster lerp on mobile for snappier feel
  const images = [];
  let loadedCount = 0;
  let lastDrawnIdx = -1;

  const sizeCanvas = () => {
    const dpr = isMobile ? 1 : Math.min(window.devicePixelRatio || 1, 2); // 1x on mobile
    canvas.width = window.innerWidth * dpr;
    canvas.height = window.innerHeight * dpr;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    lastDrawnIdx = -1;
  };
  sizeCanvas();

  let resizeTimer;
  window.addEventListener(
    "resize",
    () => {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(() => {
        sizeCanvas();
        renderFrame();
      }, 100);
    },
    { passive: true },
  );

  // ── Image Preloader ───────────────────────────────────────────────────
  // Use the correct path for the BMW images in the ASP.NET Core application
  for (let i = 0; i < TOTAL_FRAMES; i++) {
    const img = new Image();
    const pad = String(i).padStart(3, "0");
    // Use the correct path for ASP.NET Core wwwroot
    img.src = `/bm/public/asserts/bmw/bnw_${pad}.webp`;

    img.onload = img.onerror = () => {
      loadedCount++;
      if (loaderPercent)
        loaderPercent.textContent = `${Math.round((loadedCount / TOTAL_FRAMES) * 100)}%`;
      if (loadedCount === TOTAL_FRAMES) {
        ready = true;
        if (loadingScreen) loadingScreen.classList.add("done");
        renderFrame();
        startLoop();
      }
    };
    images.push(img);
  }

  // ── Draw Frame — skip if same index (huge perf win) ───────────────────
  const drawFrame = (index) => {
    if (index === lastDrawnIdx) return; // Skip redundant draws
    const img = images[index];
    if (!img || !img.complete || img.naturalWidth === 0) return;

    lastDrawnIdx = index;
    const cw = window.innerWidth,
      ch = window.innerHeight;
    const imgRatio = img.naturalWidth / img.naturalHeight;
    const canvasRatio = cw / ch;
    let dw, dh, ox, oy;

    if (canvasRatio > imgRatio) {
      dw = cw;
      dh = cw / imgRatio;
      ox = 0;
      oy = (ch - dh) / 2;
    } else {
      dh = ch;
      dw = ch * imgRatio;
      ox = (cw - dw) / 2;
      oy = 0;
    }

    ctx.drawImage(img, ox, oy, dw, dh);
  };

  const renderFrame = () => {
    if (!ready) return;
    const idx = Math.min(
      TOTAL_FRAMES - 1,
      Math.floor(currentProgress * TOTAL_FRAMES),
    );
    drawFrame(idx);
  };

  // ── Math helpers ──────────────────────────────────────────────────────
  const clamp01 = (v) => (v < 0 ? 0 : v > 1 ? 1 : v);
  const fadeInOut = (p, inS, inE, outS, outE) => {
    if (p < inS || p > outE) return 0;
    if (p <= inE) return clamp01((p - inS) / (inE - inS));
    if (p <= outS) return 1;
    return clamp01(1 - (p - outS) / (outE - outS));
  };

  // ── Update Overlays (batched style writes) ────────────────────────────
  const updateOverlays = (p) => {
    // Hero: 0%–8%
    if (overlayHero) {
      const o = p < 0.01 ? 1 : fadeInOut(p, 0, 0.01, 0.04, 0.08);
      overlayHero.style.cssText = `opacity:${o};transform:translate3d(0,${-30 * clamp01(p / 0.08)}px,0)`;
    }
    // Performance: 15%–30%
    if (overlayPerf) {
      const o = fadeInOut(p, 0.15, 0.2, 0.26, 0.3);
      const y = 20 * (1 - clamp01((p - 0.15) / 0.05));
      overlayPerf.style.cssText = `opacity:${o};transform:translate3d(0,${o > 0 ? y : 20}px,0)`;
    }
    // Design: 42%–58%
    if (overlayDesign) {
      const o = fadeInOut(p, 0.42, 0.47, 0.54, 0.58);
      const y = 20 * (1 - clamp01((p - 0.42) / 0.05));
      overlayDesign.style.cssText = `opacity:${o};transform:translate3d(0,${o > 0 ? y : 20}px,0)`;
    }
    // Specs: 65%–80%
    if (overlaySpecs) {
      const o = fadeInOut(p, 0.65, 0.7, 0.76, 0.8);
      const y = 30 * (1 - clamp01((p - 0.65) / 0.05));
      overlaySpecs.style.cssText = `opacity:${o};transform:translate3d(0,${o > 0 ? y : 30}px,0)`;
    }
    // CTA: 88%–100%
    if (overlayCta) {
      const o = clamp01((p - 0.88) / 0.06);
      overlayCta.style.cssText = `opacity:${o};transform:translate3d(0,${20 * (1 - o)}px,0)`;
    }
    // Navbar
    if (floatingNav) floatingNav.classList.toggle("hidden", p < 0.06);
    // Canvas brightness
    if (canvasWrap) {
      let b = 1;
      if (p < 0.08) b = 0.3 + 0.7 * clamp01(p / 0.08);
      else if (p > 0.85) b = 0.3 + 0.7 * (1 - clamp01((p - 0.85) / 0.1));
      canvasWrap.style.opacity = b;
    }
  };

  // ── Scroll Handler (passive, no work) ──────────────────────────────────
  window.addEventListener(
    "scroll",
    () => {
      if (!ready) return;
      const rect = scrollContainer.getBoundingClientRect();
      targetProgress = clamp01(
        -rect.top / Math.max(1, rect.height - window.innerHeight),
      );
    },
    { passive: true },
  );

  // ── Single Master Animation Loop ──────────────────────────────────────
  const startLoop = () => {
    const tick = () => {
      // Velocity-aware lerp: accelerate when far, decelerate smoothly when close
      const diff = targetProgress - currentProgress;
      const absDiff = Math.abs(diff);

      if (absDiff > 0.0001) {
        // Dynamic ease: faster catch-up when far, ultra-smooth when close
        const dynamicEase = EASE + absDiff * 0.06;
        currentProgress += diff * Math.min(dynamicEase, 0.12);
      } else {
        currentProgress = targetProgress;
      }

      // Canvas — only draw when frame changes
      renderFrame();
      updateOverlays(currentProgress);

      // Cursor glow — desktop only
      if (!isMobile && glow) {
        glowX += (mouseX - glowX) * 0.15;
        glowY += (mouseY - glowY) * 0.15;
        const w = glow.classList.contains("hover") ? 32 : 20;
        glow.style.transform = `translate3d(${glowX - w}px,${glowY - w}px,0)`;
      }

      requestAnimationFrame(tick);
    };
    tick();
  };

  // ── Initial State ─────────────────────────────────────────────────────
  if (floatingNav) floatingNav.classList.add("hidden");
  if (overlayHero) overlayHero.style.opacity = "1";
});
