document.addEventListener("DOMContentLoaded", function () {
    if (typeof flatpickr === "undefined") {
        return;
    }

    flatpickr.localize(flatpickr.l10ns.ja);

    flatpickr(".js-date-picker", {
        dateFormat: "Y-m-d",
        altInput: true,
        altFormat: "Y年m月d日",
        allowInput: true,
        locale: "ja"
    });

    flatpickr(".js-month-picker", {
        plugins: [
            new monthSelectPlugin({
                shorthand: false,
                dateFormat: "Y-m",
                altFormat: "Y年m月"
            })
        ],
        allowInput: true,
        locale: "ja"
    });
});