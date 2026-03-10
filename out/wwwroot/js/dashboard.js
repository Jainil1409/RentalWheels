// Dashboard-specific JavaScript
// Counter animations and dashboard interactions are handled by site.js
// This file is reserved for any additional dashboard-specific functionality

document.addEventListener('DOMContentLoaded', function () {
    // Stagger metric cards animation
    document.querySelectorAll('.metric-card').forEach(function (card, index) {
        card.style.animationDelay = (index * 0.1) + 's';
    });
});
