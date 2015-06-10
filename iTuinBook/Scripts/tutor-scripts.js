$(document).ready(function () {
    alert("hola");
    
    document.getElementById('iView').contentWindow.document.designMode = "on";

    $('#btnBold').click(function () {
        document.getElementById("iView").contentWindow.document.execCommand("bold", false, null);
    });   
});