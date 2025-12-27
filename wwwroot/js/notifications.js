/**
 * Notification System - Real-time Updates
 * Handles notification bell interactions, real-time updates, and API calls
 */

(function() {
    'use strict';

    let notificationUpdateInterval = null;
    const UPDATE_INTERVAL = 30000; // 30 seconds
    let isNotificationDropdownOpen = false;

    // Initialize notification system when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initializeNotificationSystem();
    });

    /**
     * Initialize notification system
     */
    function initializeNotificationSystem() {
        // Setup notification dropdown events
        setupNotificationDropdown();

        // Setup mark as read handlers
        setupMarkAsReadHandlers();

        // Setup mark all as read handler
        setupMarkAllAsReadHandler();

        // Start real-time update polling
        startNotificationPolling();

        // Update time ago labels periodically
        setInterval(updateTimeAgoLabels, 60000); // Update every minute

        // Initial time ago update
        updateTimeAgoLabels();
    }

    /**
     * Setup notification dropdown events
     */
    function setupNotificationDropdown() {
        const dropdownElement = document.getElementById('notificationDropdown');
        const dropdownMenu = document.querySelector('.notification-dropdown');

        if (!dropdownElement || !dropdownMenu) return;

        // Track dropdown state
        dropdownElement.addEventListener('shown.bs.dropdown', function() {
            isNotificationDropdownOpen = true;
        });

        dropdownElement.addEventListener('hidden.bs.dropdown', function() {
            isNotificationDropdownOpen = false;
        });

        // Mark notification as read when clicked
        const notificationItems = document.querySelectorAll('.notification-item');
        notificationItems.forEach(item => {
            item.addEventListener('click', function(e) {
                const notificationId = this.getAttribute('data-notification-id');
                if (notificationId) {
                    markAsRead(notificationId, false); // Don't prevent navigation
                }
            });
        });
    }

    /**
     * Setup mark as read handlers for notification items
     */
    function setupMarkAsReadHandlers() {
        // This is handled in setupNotificationDropdown
        // Additional handlers can be added here if needed
    }

    /**
     * Setup mark all as read handler
     */
    function setupMarkAllAsReadHandler() {
        const markAllReadBtn = document.querySelector('.mark-all-read-btn');
        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                markAllAsRead();
            });
        }
    }

    /**
     * Start polling for notification updates
     */
    function startNotificationPolling() {
        // Update immediately
        updateUnreadCount();

        // Then poll every UPDATE_INTERVAL
        notificationUpdateInterval = setInterval(function() {
            updateUnreadCount();
            
            // If dropdown is open, refresh the list
            if (isNotificationDropdownOpen) {
                // Optionally refresh the notification list here
                // For now, we just update the count
            }
        }, UPDATE_INTERVAL);
    }

    /**
     * Update unread count badge
     */
    async function updateUnreadCount() {
        try {
            const response = await fetch('/Notification/GetUnreadCount', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to fetch unread count');
            }

            const data = await response.json();
            updateNotificationBadge(data.count);
        } catch (error) {
            console.error('Error updating unread count:', error);
        }
    }

    /**
     * Update notification badge display
     */
    function updateNotificationBadge(count) {
        const badge = document.querySelector('.notification-badge');
        const dropdown = document.querySelector('#notificationDropdown');

        if (count > 0) {
            // Show badge
            if (!badge) {
                // Create badge if it doesn't exist
                const badgeHtml = `<span class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger notification-badge" 
                                     style="font-size: 0.65rem; padding: 0.25em 0.5em; animation: pulse 2s infinite;">
                                     ${count > 99 ? '99+' : count}
                                  </span>`;
                if (dropdown) {
                    dropdown.insertAdjacentHTML('beforeend', badgeHtml);
                }
            } else {
                // Update existing badge
                badge.textContent = count > 99 ? '99+' : count.toString();
                badge.style.display = 'inline-block';
            }
        } else {
            // Hide badge
            if (badge) {
                badge.style.display = 'none';
            }
        }
    }

    /**
     * Mark notification as read
     */
    async function markAsRead(notificationId, preventNavigation = false) {
        if (preventNavigation) {
            event?.preventDefault();
        }

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch(`/Notification/MarkAsRead/${notificationId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify({})
            });

            if (!response.ok) {
                throw new Error('Failed to mark notification as read');
            }

            const data = await response.json();
            
            if (data.success) {
                // Update badge
                updateNotificationBadge(data.unreadCount);

                // Remove unread styling from the notification item
                const notificationItem = document.querySelector(`[data-notification-id="${notificationId}"]`);
                if (notificationItem) {
                    notificationItem.classList.remove('unread');
                    
                    // Remove the unread indicator dot
                    const dot = notificationItem.querySelector('.badge.bg-primary.rounded-circle');
                    if (dot) {
                        dot.remove();
                    }
                }
            }
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }

    /**
     * Mark all notifications as read
     */
    async function markAllAsRead() {
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const response = await fetch('/Notification/MarkAllAsRead', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify({})
            });

            if (!response.ok) {
                throw new Error('Failed to mark all notifications as read');
            }

            const data = await response.json();
            
            if (data.success) {
                // Update badge to 0
                updateNotificationBadge(0);

                // Remove unread styling from all notification items
                const notificationItems = document.querySelectorAll('.notification-item.unread');
                notificationItems.forEach(item => {
                    item.classList.remove('unread');
                    
                    // Remove unread indicator dots
                    const dot = item.querySelector('.badge.bg-primary.rounded-circle');
                    if (dot) {
                        dot.remove();
                    }
                });

                // Hide mark all as read button
                const markAllBtn = document.querySelector('.mark-all-read-btn');
                if (markAllBtn) {
                    markAllBtn.style.display = 'none';
                }
            }
        } catch (error) {
            console.error('Error marking all notifications as read:', error);
        }
    }

    /**
     * Update time ago labels
     */
    function updateTimeAgoLabels() {
        const timeAgoElements = document.querySelectorAll('.time-ago[data-time]');
        
        timeAgoElements.forEach(element => {
            const timeString = element.getAttribute('data-time');
            if (timeString) {
                try {
                    const time = new Date(timeString);
                    const timeAgo = getTimeAgo(time);
                    element.textContent = timeAgo;
                } catch (e) {
                    console.error('Error parsing time:', e);
                }
            }
        });
    }

    /**
     * Get time ago string
     */
    function getTimeAgo(date) {
        const now = new Date();
        const diffMs = now - date;
        const diffSecs = Math.floor(diffMs / 1000);
        const diffMins = Math.floor(diffSecs / 60);
        const diffHours = Math.floor(diffMins / 60);
        const diffDays = Math.floor(diffHours / 24);
        const diffWeeks = Math.floor(diffDays / 7);
        const diffMonths = Math.floor(diffDays / 30);
        const diffYears = Math.floor(diffDays / 365);

        if (diffSecs < 60) return 'Just now';
        if (diffMins < 60) return `${diffMins} minute${diffMins !== 1 ? 's' : ''} ago`;
        if (diffHours < 24) return `${diffHours} hour${diffHours !== 1 ? 's' : ''} ago`;
        if (diffDays < 7) return `${diffDays} day${diffDays !== 1 ? 's' : ''} ago`;
        if (diffWeeks < 4) return `${diffWeeks} week${diffWeeks !== 1 ? 's' : ''} ago`;
        if (diffMonths < 12) return `${diffMonths} month${diffMonths !== 1 ? 's' : ''} ago`;
        return `${diffYears} year${diffYears !== 1 ? 's' : ''} ago`;
    }

    // Cleanup on page unload
    window.addEventListener('beforeunload', function() {
        if (notificationUpdateInterval) {
            clearInterval(notificationUpdateInterval);
        }
    });

    // Expose functions globally if needed
    window.NotificationSystem = {
        updateUnreadCount: updateUnreadCount,
        markAsRead: markAsRead,
        markAllAsRead: markAllAsRead
    };

})();

