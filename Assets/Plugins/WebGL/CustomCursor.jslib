mergeInto(LibraryManager.library, {
    ArsVerbi_SetCustomCursorActive: function (active) {
        var canvas = Module.canvas;
        if (active) {
            if (!Module.arsVerbiCursorStyle) {
                var style = document.createElement('style');
                // Override Unity's inline cursor changes only on this game's canvas.
                style.textContent = 'canvas.ars-verbi-custom-cursor { cursor: none !important; }';
                document.head.appendChild(style);
                Module.arsVerbiCursorStyle = style;
            }
            canvas.classList.add('ars-verbi-custom-cursor');
        } else {
            canvas.classList.remove('ars-verbi-custom-cursor');
            if (Module.arsVerbiCursorStyle) {
                Module.arsVerbiCursorStyle.remove();
                delete Module.arsVerbiCursorStyle;
            }
        }
    }
});
