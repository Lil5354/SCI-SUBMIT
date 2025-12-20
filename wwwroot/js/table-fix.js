// ========================================
// TABLE FIX - JavaScript fallback
// Force alignment if CSS fails
// ========================================

(function() {
    'use strict';
    
    function fixTableAlignment() {
        // Find all tables
        const tables = document.querySelectorAll('table.table');
        
        tables.forEach(function(table) {
            // Fix all th elements
            const ths = table.querySelectorAll('thead th');
            ths.forEach(function(th, index) {
                th.style.verticalAlign = 'middle';
                th.style.padding = '12px';
                
                // CRITICAL: Fix first column (STT/#) to ensure it displays
                if (index === 0) {
                    th.style.width = '5%';
                    th.style.minWidth = '50px';
                    th.style.maxWidth = '60px';
                    th.style.display = 'table-cell';
                    th.style.visibility = 'visible';
                    th.style.opacity = '1';
                }
            });
            
            // Fix all td elements
            const tds = table.querySelectorAll('tbody td');
            tds.forEach(function(td, index) {
                td.style.verticalAlign = 'middle';
                td.style.padding = '12px';
                
                // CRITICAL: Fix first column (STT/#) to ensure it displays
                const row = td.parentElement;
                const rowIndex = Array.from(row.cells).indexOf(td);
                if (rowIndex === 0) {
                    td.style.width = '5%';
                    td.style.minWidth = '50px';
                    td.style.maxWidth = '60px';
                    td.style.display = 'table-cell';
                    td.style.visibility = 'visible';
                    td.style.opacity = '1';
                    td.style.textAlign = 'center';
                    
                    // Ensure content is visible and never empty
                    const textContent = td.textContent.trim();
                    const innerHTML = td.innerHTML.trim();
                    if (!textContent && (!innerHTML || innerHTML === '' || innerHTML === '<br>' || innerHTML === '<br/>')) {
                        const rowNumber = Array.from(row.parentElement.children).indexOf(row) + 1;
                        td.textContent = rowNumber.toString();
                    }
                    
                    // Force display
                    td.style.display = 'table-cell';
                    td.style.visibility = 'visible';
                    td.style.opacity = '1';
                    td.style.width = '5%';
                    td.style.minWidth = '50px';
                    td.style.maxWidth = '60px';
                }
            });
        });
    }
    
    // Run on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', fixTableAlignment);
    } else {
        fixTableAlignment();
    }
    
    // Run again after a short delay to catch dynamically loaded content
    setTimeout(fixTableAlignment, 100);
    setTimeout(fixTableAlignment, 500);
})();
