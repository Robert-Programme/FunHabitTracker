// site.js
document.addEventListener('DOMContentLoaded', function() {
    const habitForm = document.getElementById('habit-form');
    const habitList = document.getElementById('habit-list');

    habitForm.addEventListener('submit', function(event) {
        event.preventDefault();
        const habitName = document.getElementById('habit-name').value;
        const habitDescription = document.getElementById('habit-description').value;

        if (habitName) {
            addHabitToList(habitName, habitDescription);
            habitForm.reset();
        }
    });

    function addHabitToList(name, description) {
        const listItem = document.createElement('li');
        listItem.classList.add('habit-item');
        listItem.innerHTML = `<strong>${name}</strong>: ${description} <button class="complete-btn">✔️</button>`;
        
        listItem.querySelector('.complete-btn').addEventListener('click', function() {
            listItem.classList.toggle('completed');
        });

        habitList.appendChild(listItem);
    }
});