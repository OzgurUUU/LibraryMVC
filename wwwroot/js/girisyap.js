
const safe_password = /^[a-zA-Z0-9]+$/;
document.addEventListener('DOMContentLoaded', function() {
  const inputUsername = document.getElementById('username');
  const inputPassword = document.getElementById('password');

  // Diğer tüm kodunuz buraya
  if (inputUsername) { // Elementin gerçekten var olduğunu kontrol edin
      inputUsername.addEventListener('input', checkUsername());
  }
  // ...
});
 function checkUsername() {
    if (this.value.trim().length > 0) {
        
        inputPassword.disabled = false;

    } else {
        
        inputPassword.disabled = true;
        inputPassword.value = '';
        
    }

}
function checkPassword(inputPassword) {
    const min_pass_length = 8;
    const password = inputPassword.value;
    const isSafeCharacters = safe_password.test(password);
    if(password.length === 0 ){
        alert("Lütfen Şifrenizi Giriniz.");
        return false;
    }
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

