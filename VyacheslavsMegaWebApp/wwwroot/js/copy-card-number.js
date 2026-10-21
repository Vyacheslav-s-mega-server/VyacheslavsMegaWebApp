function copyCardNumber(event, cardNumber) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    const target = event ? (event.target.closest('.card__copy-link, .card-copy-pill, .donation-card__copy-btn, button') || event.target) : null;
    const oldHtml = target ? target.innerHTML : '';

    navigator.clipboard.writeText(String(cardNumber)).then(function () {
        if (target) {
            target.innerHTML = "<i class='bx bx-check'></i> <span>Скопировано!</span>";
            target.classList.add('is-copied');
            setTimeout(function () {
                target.innerHTML = oldHtml || "<i class='bx bx-copy'></i> <span>Скопировать номер</span>";
                target.classList.remove('is-copied');
            }, 2500);
        }
    });
}