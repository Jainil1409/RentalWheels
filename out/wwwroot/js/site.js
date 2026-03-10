// RentWheels Modern UI - Animations & Interactions

document.addEventListener('DOMContentLoaded', function () {
    // Navbar scroll effect
    const navbar = document.querySelector('.vrms-navbar');
    if (navbar) {
        window.addEventListener('scroll', function () {
            if (window.scrollY > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });
    }

    // Scroll animations using IntersectionObserver
    const scrollElements = document.querySelectorAll('.scroll-animate');
    if (scrollElements.length > 0) {
        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });

        scrollElements.forEach(function (el) {
            observer.observe(el);
        });
    }

    // Format number with Indian comma notation (e.g. 1,00,000.00)
    function formatIndian(num, decimals) {
        var parts = num.toFixed(decimals).split('.');
        var intPart = parts[0];
        var lastThree = intPart.slice(-3);
        var rest = intPart.slice(0, -3);
        if (rest.length > 0) {
            lastThree = ',' + lastThree;
        }
        var formatted = rest.replace(/\B(?=(\d{2})+(?!\d))/g, ',') + lastThree;
        return decimals > 0 ? formatted + '.' + parts[1] : formatted;
    }

    // Counter animation for dashboard stat values
    const counters = document.querySelectorAll('[data-count]');
    counters.forEach(function (counter) {
        const target = parseFloat(counter.getAttribute('data-count'));
        const prefix = counter.getAttribute('data-prefix') || '';
        const suffix = counter.getAttribute('data-suffix') || '';
        const decimals = counter.getAttribute('data-decimals') ? parseInt(counter.getAttribute('data-decimals')) : 0;
        const duration = 1500;
        const startTime = performance.now();

        function updateCounter(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3); // ease-out cubic
            const current = target * eased;
            counter.textContent = prefix + formatIndian(current, decimals) + suffix;
            if (progress < 1) {
                requestAnimationFrame(updateCounter);
            }
        }

        const counterObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    requestAnimationFrame(updateCounter);
                    counterObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.5 });

        counterObserver.observe(counter);
    });

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) {
                alert.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-10px)';
                setTimeout(function () { bsAlert.close(); }, 500);
            }
        }, 5000);
    });

    // Ripple effect on buttons
    document.querySelectorAll('.btn').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = btn.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            ripple.style.cssText = 'position:absolute;border-radius:50%;background:rgba(255,255,255,0.3);transform:scale(0);animation:ripple-effect 0.6s linear;pointer-events:none;width:' + size + 'px;height:' + size + 'px;left:' + (e.clientX - rect.left - size / 2) + 'px;top:' + (e.clientY - rect.top - size / 2) + 'px;';
            btn.appendChild(ripple);
            setTimeout(function () { ripple.remove(); }, 600);
        });
    });

    // Add ripple keyframe
    if (!document.querySelector('#ripple-style')) {
        const style = document.createElement('style');
        style.id = 'ripple-style';
        style.textContent = '@keyframes ripple-effect{to{transform:scale(4);opacity:0;}}';
        document.head.appendChild(style);
    }

    // Stagger animation for cards in a row
    document.querySelectorAll('.row').forEach(function (row) {
        const cards = row.querySelectorAll('.scroll-animate');
        cards.forEach(function (card, index) {
            card.style.transitionDelay = (index * 0.1) + 's';
        });
    });

    // Image error fallback for vehicle images
    document.querySelectorAll('.vehicle-img img, .detail-img-container img').forEach(function (img) {
        img.addEventListener('error', function () {
            const parent = img.parentElement;
            const placeholder = document.createElement('div');
            placeholder.className = 'vehicle-img-placeholder default';
            placeholder.innerHTML = '<i class="bi bi-car-front-fill"></i>';
            placeholder.style.height = parent.style.height || img.style.height || '220px';
            parent.replaceChild(placeholder, img);
        });
    });

    // === Admin Sidebar Logic ===
    const sidebar = document.getElementById('adminSidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarClose = document.getElementById('sidebarClose');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    if (sidebar) {
        // Mobile toggle
        if (sidebarToggle) {
            sidebarToggle.addEventListener('click', function () {
                sidebar.classList.add('show');
                if (sidebarOverlay) sidebarOverlay.classList.add('show');
            });
        }

        // Close sidebar
        function closeSidebar() {
            sidebar.classList.remove('show');
            if (sidebarOverlay) sidebarOverlay.classList.remove('show');
        }

        if (sidebarClose) sidebarClose.addEventListener('click', closeSidebar);
        if (sidebarOverlay) sidebarOverlay.addEventListener('click', closeSidebar);

        // Restore sidebar scroll position from sessionStorage
        var sidebarNav = sidebar.querySelector('.sidebar-nav');
        if (sidebarNav) {
            var savedScroll = sessionStorage.getItem('sidebarScrollPos');
            if (savedScroll !== null) {
                sidebarNav.scrollTop = parseInt(savedScroll, 10);
            }

            // Save sidebar scroll position before navigating away
            window.addEventListener('beforeunload', function () {
                sessionStorage.setItem('sidebarScrollPos', sidebarNav.scrollTop);
            });
        }
    }
});
