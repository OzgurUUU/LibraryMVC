const titleInput = document.getElementById("title");
const authorInput = document.getElementById("author");
const yearInput = document.getElementById("year");
const imageInput = document.getElementById("image");
const klasikCheckbox = document.getElementById("klasik");
const romanCheckbox = document.getElementById("roman");
const hikayeCheckbox = document.getElementById("hikaye");
const bookForm = document.getElementById("bookForm");

titleInput.addEventListener("input", function () {
    if (this.value.trim().length > 0) {
        authorInput.disabled = false;
        authorInput.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(authorInput);
    }
});

authorInput.addEventListener("input", function () {
    if (this.value.trim().length > 0) {
        yearInput.disabled = false;
        yearInput.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(yearInput);
    }
});

yearInput.addEventListener("input", function () {
    const year = parseInt(this.value);
    const currentYear = new Date().getFullYear();

    if (this.value.trim().length > 0 && year >= 1500 && year <= currentYear) {
        imageInput.disabled = false;
        imageInput.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(imageInput);
        if (year > currentYear) {
            alert("⚠️ Yayın yılı gelecek bir tarih olamaz!");
            this.value = "";
        }
    }
});

imageInput.addEventListener("input", function () {
    if (this.value.trim().length > 0 && isValidUrl(this.value)) {
        klasikCheckbox.disabled = false;
        romanCheckbox.disabled = false;
        hikayeCheckbox.disabled = false;
        const genreStep = document.querySelector(".form-step:nth-child(5)");
        if (genreStep) genreStep.style.opacity = "1";
    } else {
        klasikCheckbox.disabled = true;
        romanCheckbox.disabled = true;
        hikayeCheckbox.disabled = true;
        klasikCheckbox.checked = false;
        romanCheckbox.checked = false;
        hikayeCheckbox.checked = false;
        const genreStep = document.querySelector(".form-step:nth-child(5)");
        if (genreStep) genreStep.style.opacity = "0.5";
    }
});

bookForm.addEventListener("submit", function (e) {
    e.preventDefault();

    if (!validateForm()) {
        return;
    }

    const bookData = {
        title: titleInput.value.trim(),
        author: authorInput.value.trim(),
        year: yearInput.value,
        image: imageInput.value.trim(),
        genres: getSelectedGenres(),
    };

    addBookToTable(bookData);

    updateStatistics();

    showSuccessMessage(bookData);

    this.reset();
    resetAllFields();
});

function validateForm() {
    if (titleInput.value.trim() === "") {
        showError("Lütfen kitap adını girin!", titleInput);
        return false;
    }

    if (authorInput.value.trim() === "") {
        showError("Lütfen yazar adını girin!", authorInput);
        return false;
    }

    const year = parseInt(yearInput.value);
    const currentYear = new Date().getFullYear();

    if (yearInput.value === "" || year < 1500 || year > currentYear) {
        showError(
            "Lütfen geçerli bir yayın yılı girin (1500-" + currentYear + ")!",
            yearInput
        );
        return false;
    }

    if (imageInput.value.trim() === "" || !isValidUrl(imageInput.value)) {
        showError("Lütfen geçerli bir URL adresi girin!", imageInput);
        return false;
    }

    return true;
}

function isValidUrl(string) {
    try {
        new URL(string);
        return true;
    } catch (_) {
        return false;
    }
}

function getSelectedGenres() {
    const genres = [];
    if (klasikCheckbox.checked) genres.push("Klasik");
    if (romanCheckbox.checked) genres.push("Roman");
    if (hikayeCheckbox.checked) genres.push("Hikaye");
    return genres;
}

function addBookToTable(bookData) {
    const tableBody = document.getElementById("bookTableBody");
    const newRow = document.createElement("tr");

    const genreText =
        bookData.genres.length > 0 ? bookData.genres.join(", ") : "Belirtilmemiş";

    newRow.innerHTML = `
        <td>
            <img src="${bookData.image}" 
                 alt="${bookData.title}" 
                 class="book-cover"
                 onerror="this.src='https://via.placeholder.com/60x80?text=No+Image'">
        </td>
        <td>
            <strong>${bookData.title}</strong>
            <br><small class="text-muted">${genreText}</small>
        </td>
        <td>
            <small>${bookData.author}</small>
        </td>
        <td>
            <span class="badge badge-primary">${bookData.year}</span>
        </td>
    `;

    newRow.style.opacity = "0";
    tableBody.insertBefore(newRow, tableBody.firstChild);

    setTimeout(() => {
        newRow.style.transition = "opacity 0.5s ease";
        newRow.style.opacity = "1";
    }, 10);
}

function updateStatistics() {
    const tableBody = document.getElementById("bookTableBody");
    const totalBooks = tableBody.querySelectorAll("tr").length;

    const authors = new Set();
    const genres = new Set();

    tableBody.querySelectorAll("tr").forEach((row) => {
        const authorCell = row.querySelector("td:nth-child(3)");
        const genreCell = row.querySelector("td:nth-child(2) small");

        if (authorCell) {
            authors.add(authorCell.textContent.trim());
        }

        if (genreCell && genreCell.textContent !== "Belirtilmemiş") {
            const bookGenres = genreCell.textContent.split(",").map((g) => g.trim());
            bookGenres.forEach((g) => genres.add(g));
        }
    });

    document.querySelector(".stat-books .stat-number").textContent = totalBooks;
    document.querySelector(".stat-authors .stat-number").textContent =
        authors.size;
    document.querySelector(".stat-genres .stat-number").textContent = genres.size;
}

function showError(message, field) {
    alert("⚠️ " + message);
    field.focus();
}

function showSuccessMessage(bookData) {
    const genreText =
        bookData.genres.length > 0 ? bookData.genres.join(", ") : "Belirtilmemiş";
    alert(
        `✅ Başarılı!\n\nKitap: ${bookData.title}\nYazar: ${bookData.author}\nYıl: ${bookData.year}\nTür: ${genreText}\n\nKitap başarıyla eklendi!`
    );
}

function resetForm(fromField) {
    const fields = [authorInput, yearInput, imageInput];
    const checkboxes = [klasikCheckbox, romanCheckbox, hikayeCheckbox];
    const startIndex = fields.indexOf(fromField);

    for (let i = startIndex; i < fields.length; i++) {
        fields[i].disabled = true;
        fields[i].value = "";
        if (fields[i].parentElement && fields[i].parentElement.parentElement) {
            fields[i].parentElement.parentElement.style.opacity = "0.5";
        }
    }

    checkboxes.forEach((checkbox) => {
        checkbox.disabled = true;
        checkbox.checked = false;
    });

    const genreStep = document.querySelector(".form-step:nth-child(5)");
    if (genreStep) genreStep.style.opacity = "0.5";
}

function resetAllFields() {
    const allFields = [authorInput, yearInput, imageInput];
    const checkboxes = [klasikCheckbox, romanCheckbox, hikayeCheckbox];

    allFields.forEach((field) => {
        field.disabled = true;
        field.value = "";
        if (field.parentElement && field.parentElement.parentElement) {
            field.parentElement.parentElement.style.opacity = "0.5";
        }
    });

    checkboxes.forEach((checkbox) => {
        checkbox.disabled = true;
        checkbox.checked = false;
    });

    const genreStep = document.querySelector(".form-step:nth-child(5)");
    if (genreStep) genreStep.style.opacity = "0.5";
}

window.addEventListener("DOMContentLoaded", function () {
    const allSteps = document.querySelectorAll(".form-step");
    allSteps.forEach((step, index) => {
        if (index > 0) {
            step.style.opacity = "0.5";
        }
    });

    updateStatistics();
});
