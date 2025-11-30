const sayi1 = Math.floor(Math.random() * 10) + 1;
const sayi2 = Math.floor(Math.random() * 10) + 1;
const dogruCevap = sayi1 + sayi2;

const input2 = document.getElementById("kayit-btn")
document.getElementById("soru").innerText = `${sayi1} + ${sayi2} = ?`;

const input = document.getElementById("cevap");
const uyari = document.getElementById("uyari");
input2.addEventListener("click", () => {
    const cevap = parseInt(input.value);
    
    const ready = checkPassword();
    console.log(ready);
    if (ready) {
        if (cevap === dogruCevap) {
            console.log("Doğru")
            uyari.textContent = "✅ Doğru! Artık kayıt olabilirsin.";
            uyari.style.color = "lightgreen";

            setTimeout(() => {
                window.location.href = "/Home/Login";
            }, 500);
        } else {
            uyari.textContent = "❌ Yanlış cevap, lütfen tekrar dene.";
            uyari.style.color = "rgb(255, 153, 153)";
        }
    }


});
const nickney = document.getElementById("numara")

nickney.addEventListener("keypress", function (event) {

    console.log("")

    if (!/[0-9]/.test(event.key))
        event.preventDefault();
});
