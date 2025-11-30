const safe_password = /^[a-zA-Z0-9]+$/;
const inputUsername = document.getElementById('username');
const inputPassword = document.getElementById('password');

document.addEventListener('DOMContentLoaded', function () {

    if (inputUsername) {
        inputUsername.addEventListener('input', () => {
            checkUsername(inputUsername, inputPassword);
        });
    }
});
function checkUsername(inputUsername, inputPassword) {

    if (inputUsername.value.trim().length > 0) {

        inputPassword.disabled = false;

    } else {

        inputPassword.disabled = true;
        inputPassword.value = '';

    }
}
function checkPassword() {
    const min_pass_length = 8;
    const password = inputPassword.value;
    if (password.length === 0) {
        alert("Lütfen Şifrenizi Giriniz.");
        return false;
    }
    const isSafeCharacters = safe_password.test(password);

    if (!isSafeCharacters) {
        alert("Şifre yalnızca harf ve rakamlardan oluşabilir.");
        return false;
    }
    if (password.length < min_pass_length) {
        alert(`Şifre en az ${min_pass_length} karakter uzunluğunda olmalıdır.`);

        return false;
    }
    return true;
}

