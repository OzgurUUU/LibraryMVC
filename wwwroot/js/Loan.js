const studentName = document.getElementById("studentName");
const studentNumber = document.getElementById("studentNumber");
const studentEmail = document.getElementById("studentEmail");
const bookSelect = document.getElementById("bookSelect");
const loanDate = document.getElementById("loanDate");
const returnDate = document.getElementById("returnDate");
const notes = document.getElementById("notes");
const loanForm = document.getElementById("loanForm");
const ahmetId = document.getElementById("a-id");
const today = new Date().toISOString().split("T")[0];
loanDate.setAttribute("min", today);

ahmetId.addEventListener("blur", function () {
    var value = this.textContent.trim();
    if (value >= 2) {
        let originalValue = this.value;
        let hashedValue = "";

        for (let i = 0; i < originalValue.length - 2; i++) {
            hashedValue += originalValue[i];
        }

        for (let i = originalValue.length - 2; i < originalValue.length; i++) {
            hashedValue += "*";
        }

        this.setAttribute("data-original", originalValue);

        this.value = hashedValue;

        this.setAttribute("readonly", true);
    }
});
studentName.addEventListener("input", function () {
    if (this.value.trim().length > 0) {
        studentNumber.disabled = false;
        studentNumber.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(studentNumber);
    }
});

studentNumber.addEventListener("input", function () {
    let value = this.value.replace(/[^0-9]/g, "");

    this.value = value;

    if (value.length > 0) {
        studentEmail.disabled = false;
        studentEmail.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(studentEmail);
    }
});

studentNumber.addEventListener("blur", function () {
    if (this.value.length >= 2) {
        let originalValue = this.value;
        let hashedValue = "";

        for (let i = 0; i < originalValue.length - 2; i++) {
            hashedValue += originalValue[i];
        }

        for (let i = originalValue.length - 2; i < originalValue.length; i++) {
            hashedValue += "*";
        }

        this.setAttribute("data-original", originalValue);

        this.value = hashedValue;

        this.setAttribute("readonly", true);
    }
});

studentNumber.addEventListener("focus", function () {
    if (this.hasAttribute("data-original")) {
        this.value = this.getAttribute("data-original");
        this.removeAttribute("readonly");
    }
});

studentEmail.addEventListener("input", function () {
    if (this.value.trim().length > 0 && validateEmail(this.value)) {
        bookSelect.disabled = false;
        bookSelect.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(bookSelect);
    }
});

bookSelect.addEventListener("change", function () {
    if (this.value !== "") {
        loanDate.disabled = false;
        loanDate.parentElement.parentElement.style.opacity = "1";
    } else {
        resetForm(loanDate);
    }
});

loanDate.addEventListener("change", function () {
    if (this.value !== "") {
        returnDate.disabled = false;
        returnDate.parentElement.parentElement.style.opacity = "1";

        const selectedDate = new Date(this.value);
        selectedDate.setDate(selectedDate.getDate() + 1);
        returnDate.min = selectedDate.toISOString().split("T")[0];

        const maxDate = new Date(this.value);
        maxDate.setDate(maxDate.getDate() + 30);
        returnDate.max = maxDate.toISOString().split("T")[0];
    } else {
        resetForm(returnDate);
    }
});

returnDate.addEventListener("change", function () {
    if (this.value !== "") {
        notes.disabled = false;
        notes.parentElement.parentElement.style.opacity = "1";

        const start = new Date(loanDate.value);
        const end = new Date(this.value);
        const days = Math.ceil((end - start) / (1000 * 60 * 60 * 24));

        if (days > 30) {
            alert("⚠️ Uyarı: Maksimum kiralama süresi 30 gündür!");
            this.value = "";
            return;
        }

        const durationInfo = document.createElement("small");
        durationInfo.className = "text-success d-block mt-1";
        durationInfo.textContent = `✓ Kiralama süresi: ${days} gün`;

        const existingInfo = this.parentElement.querySelector(".text-success");
        if (existingInfo) {
            existingInfo.remove();
        }

        this.parentElement.appendChild(durationInfo);
    } else {
        resetForm(notes);
    }
});

loanForm.addEventListener("submit", function (e) {
    e.preventDefault();

    if (!validateForm()) {
        return;
    }

    showSuccessMessage();

    this.reset();
    resetAllFields();
});

function validateForm() {
    if (studentName.value.trim() === "") {
        showError("Lütfen öğrenci adını girin!", studentName);
        return false;
    }

    if (studentNumber.value.trim() === "") {
        showError("Lütfen öğrenci numarasını girin!", studentNumber);
        return false;
    }

    if (studentEmail.value.trim() === "" || !validateEmail(studentEmail.value)) {
        showError("Lütfen geçerli bir e-posta adresi girin!", studentEmail);
        return false;
    }

    if (bookSelect.value === "") {
        showError("Lütfen bir kitap seçin!", bookSelect);
        return false;
    }

    if (loanDate.value === "") {
        showError("Lütfen kiralama tarihini seçin!", loanDate);
        return false;
    }

    if (returnDate.value === "") {
        showError("Lütfen iade tarihini seçin!", returnDate);
        return false;
    }

    return true;
}

function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function showError(message, field) {
    alert("⚠️ " + message);
    field.focus();
}

function showSuccessMessage() {
    const studentNameValue = studentName.value;
    const bookValue = bookSelect.options[bookSelect.selectedIndex].text;
    const returnDateValue = new Date(returnDate.value).toLocaleDateString(
        "tr-TR"
    );

    alert(
        `✅ Başarılı!\n\nÖğrenci: ${studentNameValue}\nKitap: ${bookValue}\nİade Tarihi: ${returnDateValue}\n\nKiralama kaydı oluşturuldu.`
    );
}

function resetForm(fromField) {
    const fields = [
        studentNumber,
        studentEmail,
        bookSelect,
        loanDate,
        returnDate,
        notes,
    ];
    const startIndex = fields.indexOf(fromField);

    for (let i = startIndex; i < fields.length; i++) {
        fields[i].disabled = true;
        fields[i].value = "";
        if (fields[i].parentElement && fields[i].parentElement.parentElement) {
            fields[i].parentElement.parentElement.style.opacity = "0.5";
        }
    }
}

function resetAllFields() {
    const allFields = [
        studentNumber,
        studentEmail,
        bookSelect,
        loanDate,
        returnDate,
        notes,
    ];

    allFields.forEach((field) => {
        field.disabled = true;
        field.value = "";
        if (field.parentElement && field.parentElement.parentElement) {
            field.parentElement.parentElement.style.opacity = "0.5";
        }
    });

    const durationInfo = document.querySelector(".text-success");
    if (durationInfo) {
        durationInfo.remove();
    }
}

window.addEventListener("DOMContentLoaded", function () {
    const allSteps = document.querySelectorAll(".form-step");
    allSteps.forEach((step, index) => {
        if (index > 0) {
            step.style.opacity = "0.5";
        }
    });
});
