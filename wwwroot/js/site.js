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

    function initUI() {
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

        // Counter animation for dashboard stat values
        const counters = document.querySelectorAll('[data-count]:not([data-counted])');
        counters.forEach(function (counter) {
            counter.setAttribute('data-counted', 'true');
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
        const alerts = document.querySelectorAll('.alert-dismissible:not([data-alert-attached])');
        alerts.forEach(function (alert) {
            alert.setAttribute('data-alert-attached', 'true');
            setTimeout(function () {
                const bsAlert = typeof bootstrap !== 'undefined' ? bootstrap.Alert.getOrCreateInstance(alert) : null;
                if (bsAlert) {
                    alert.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
                    alert.style.opacity = '0';
                    alert.style.transform = 'translateY(-10px)';
                    setTimeout(function () { bsAlert.close(); }, 500);
                }
            }, 5000);
        });

        // Ripple effect on buttons
        document.querySelectorAll('.btn:not([data-ripple-attached])').forEach(function (btn) {
            btn.setAttribute('data-ripple-attached', 'true');
            btn.addEventListener('click', function (e) {
                const ripple = document.createElement('span');
                const rect = btn.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                ripple.style.cssText = 'position:absolute;border-radius:50%;background:rgba(255,255,255,0.3);transform:scale(0);animation:ripple-effect 0.6s linear;pointer-events:none;width:' + size + 'px;height:' + size + 'px;left:' + (e.clientX - rect.left - size / 2) + 'px;top:' + (e.clientY - rect.top - size / 2) + 'px;';
                btn.appendChild(ripple);
                setTimeout(function () { ripple.remove(); }, 600);
            });
        });
        
        // Stagger animation for cards in a row
        document.querySelectorAll('.row').forEach(function (row) {
            if (row.hasAttribute('data-stagger-attached')) return;
            row.setAttribute('data-stagger-attached', 'true');
            const cards = row.querySelectorAll('.scroll-animate');
            cards.forEach(function (card, index) {
                card.style.transitionDelay = (index * 0.1) + 's';
            });
        });

        // Image error fallback for vehicle images
        document.querySelectorAll('.vehicle-img img:not([data-error-attached]), .detail-img-container img:not([data-error-attached])').forEach(function (img) {
            img.setAttribute('data-error-attached', 'true');
            img.addEventListener('error', function () {
                const parent = img.parentElement;
                const placeholder = document.createElement('div');
                placeholder.className = 'vehicle-img-placeholder default';
                placeholder.innerHTML = '<i class="bi bi-car-front-fill"></i>';
                placeholder.style.height = parent.style.height || img.style.height || '220px';
                parent.replaceChild(placeholder, img);
            });
        });
    }

    // Call normally on initial load
    initUI();

    // === TURBO-STYLE ADMIN AJAX CONTENT (Prevent Full Page Refresh) ===
    function performAjaxNavigation(url, method = 'GET', body = null) {
        // Show global loader to indicate processing
        const loader = document.getElementById('global-loader');
        if(loader) loader.classList.add('show');

        // Add nice transition class to main body
        const currentMain = document.querySelector('.admin-main-content');
        if (currentMain) {
            currentMain.style.transition = 'opacity 0.2s';
            currentMain.style.opacity = '0.4';
        }

        const options = { method: method };
        if (body) options.body = body;

        // Perform AJAX fetch
        fetch(url, options)
            .then(response => {
                if(response.redirected) {
                    window.location.href = response.url;
                    return null;
                }
                return response.text();
            })
            .then(html => {
                if (!html) return;
                
                // Parse fetched HTML
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');

                // Update Main Content
                const newMain = doc.querySelector('.admin-main-content');
                
                if (newMain && currentMain) {
                    currentMain.innerHTML = newMain.innerHTML;
                    // Update Title
                    document.title = doc.title;
                    
                    if (method === 'GET') {
                        window.history.pushState({path: url}, doc.title, url);
                    }
                    
                    // Re-evaluate scripts in the new content
                    const scripts = currentMain.querySelectorAll('script');
                    scripts.forEach(oldScript => {
                        const newScript = document.createElement('script');
                        Array.from(oldScript.attributes).forEach(attr => newScript.setAttribute(attr.name, attr.value));
                        newScript.appendChild(document.createTextNode(oldScript.innerHTML));
                        oldScript.parentNode.replaceChild(newScript, oldScript);
                    });

                    // Remove loader & reset opacity
                    if(loader) setTimeout(() => loader.classList.remove('show'), 150);
                    currentMain.style.opacity = '1';
                    
                    // Scroll to top of main content
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                    
                    // Re-init Bootstrap components
                    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
                        const tooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
                        tooltips.map(function (t) { return new bootstrap.Tooltip(t) });
                    }
                    
                    // Re-init RentWheels UI
                    setTimeout(initUI, 50);
                } else {
                    window.location.href = url;
                }
            })
            .catch(err => {
                console.error('Navigation error:', err);
                window.location.href = url;
            });
    }

    // Event delegation for links and pagination inside the admin area
    document.addEventListener('click', function (e) {
        const sidebarLink = e.target.closest('.admin-sidebar .sidebar-link');
        const contentLink = e.target.closest('.admin-main-content .pagination a, .admin-main-content th a, .admin-main-content a.page-link');

        const link = sidebarLink || contentLink;
        
        if (link) {
            if (link.classList.contains('sidebar-logout-btn')) return;
            if (e.ctrlKey || e.metaKey || e.shiftKey || link.target === '_blank') return;
            
            let url = link.getAttribute('href');
            if (!url || url === '#' || url.startsWith('javascript:')) return;

            e.preventDefault();

            if (sidebarLink) {
                document.querySelectorAll('.admin-sidebar .sidebar-link').forEach(l => l.classList.remove('active'));
                sidebarLink.classList.add('active');
            }

            performAjaxNavigation(url);
        }
    });

    // Intercept Search / Filter forms inside admin-main-content
    document.addEventListener('submit', function (e) {
        const form = e.target.closest('.admin-main-content form');
        if (form && form.method.toUpperCase() === 'GET') {
            e.preventDefault();
            const formData = new FormData(form);
            const params = new URLSearchParams(formData);
            const action = form.getAttribute('action') || window.location.pathname;
            const url = action + (action.includes('?') ? '&' : '?') + params.toString();
            performAjaxNavigation(url);
        }
    });

    // Handle browser back/forward buttons
    window.addEventListener('popstate', function(e) {
        window.location.reload();
    });

    // Add ripple keyframe
    if (!document.querySelector('#ripple-style')) {
        const style = document.createElement('style');
        style.id = 'ripple-style';
        style.textContent = '@keyframes ripple-effect{to{transform:scale(4);opacity:0;}}';
        document.head.appendChild(style);
    }

    // === Admin Sidebar Logic ===
    const sidebar = document.getElementById('adminSidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarClose = document.getElementById('sidebarClose');
    const sidebarOverlay = document.getElementById('sidebarOverlay');

    if (sidebar) {
        if (sidebarToggle) {
            sidebarToggle.addEventListener('click', function () {
                sidebar.classList.add('show');
                if (sidebarOverlay) sidebarOverlay.classList.add('show');
            });
        }

        function closeSidebar() {
            sidebar.classList.remove('show');
            if (sidebarOverlay) sidebarOverlay.classList.remove('show');
        }

        if (sidebarClose) sidebarClose.addEventListener('click', closeSidebar);
        if (sidebarOverlay) sidebarOverlay.addEventListener('click', closeSidebar);

        var sidebarNav = sidebar.querySelector('.sidebar-nav');
        if (sidebarNav) {
            var savedScroll = sessionStorage.getItem('sidebarScrollPos');
            if (savedScroll !== null) {
                sidebarNav.scrollTop = parseInt(savedScroll, 10);
            }

            window.addEventListener('beforeunload', function () {
                sessionStorage.setItem('sidebarScrollPos', sidebarNav.scrollTop);
            });
        }
    }

    // === Global Loading Indicator Logic ===
    let loaderTimeout = null;

    function showGlobalLoader() {
        const loader = document.getElementById('global-loader');
        if (loader) {
            loader.classList.add('show');
        }
        // Safety: auto-hide after 8 seconds in case navigation doesn't complete
        if (loaderTimeout) clearTimeout(loaderTimeout);
        loaderTimeout = setTimeout(hideGlobalLoader, 8000);
    }

    function hideGlobalLoader() {
        const loader = document.getElementById('global-loader');
        if (loader) {
            loader.classList.remove('show');
        }
        if (loaderTimeout) {
            clearTimeout(loaderTimeout);
            loaderTimeout = null;
        }
    }

    // Hide loader when page is shown (handles back/forward navigation & bfcache)
    window.addEventListener('pageshow', function (e) {
        hideGlobalLoader();
    });

    // Hide loader when page becomes visible again (handles tab switching, back navigation)
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            hideGlobalLoader();
        }
    });

    // Also hide on load as a final safety net
    window.addEventListener('load', function () {
        hideGlobalLoader();
    });

    document.addEventListener('submit', function (e) {
        if (!e.target.hasAttribute('data-no-loader') && e.target.getAttribute('target') !== '_blank') {
            if (typeof $(e.target).valid === 'function') {
                if ($(e.target).valid()) {
                    showGlobalLoader();
                }
            } else {
                showGlobalLoader();
            }
        }
    });

    document.addEventListener('click', function (e) {
        const link = e.target.closest('a');
        if (link && link.href) {
            const isTargetBlank = link.getAttribute('target') === '_blank';
            const isDownload = link.hasAttribute('download');
            const isHash = link.href.indexOf('#') !== -1 && link.href.split('#')[0] === window.location.href.split('#')[0];
            const isJs = link.href.startsWith('javascript:');
            const hasNoLoader = link.hasAttribute('data-no-loader');
            const isToggle = link.hasAttribute('data-bs-toggle') || link.hasAttribute('data-toggle');
            const isNullOrEmpty = link.getAttribute('href') === '' || link.getAttribute('href') === '#';

            if (!isTargetBlank && !isDownload && !isHash && !isJs && !hasNoLoader && !isToggle && !isNullOrEmpty) {
                showGlobalLoader();
            }
        }
    });
});
