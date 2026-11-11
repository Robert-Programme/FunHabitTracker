document.addEventListener('DOMContentLoaded', () => {
    const recurrence = document.querySelector('[data-recurrence]');
    const weekdays = document.querySelector('[data-weekdays]');
    if (!recurrence || !weekdays) return;
    const update = () => { weekdays.hidden = recurrence.value !== 'SelectedWeekdays'; };
    recurrence.addEventListener('change', update);
    update();
});

const bindCompactForms = () => {
    document.querySelectorAll('[data-compact-outcome]').forEach((form) => {
        if (form.dataset.enhanced === 'true') return;
        form.dataset.enhanced = 'true';
        form.addEventListener('submit', async (event) => {
            event.preventDefault();
            const button = form.querySelector('button');
            if (button) button.disabled = true;
            try {
                const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
                if (!response.ok) throw new Error('Outcome update failed');
                const next = new DOMParser().parseFromString(await response.text(), 'text/html').querySelector('.compact-shell');
                const current = document.querySelector('.compact-shell');
                if (!next || !current) throw new Error('Compact response is invalid');
                current.replaceWith(next);
                bindCompactForms();
            } catch {
                form.submit();
            }
        });
    });
};

document.addEventListener('DOMContentLoaded', bindCompactForms);
