//secuencias-controller.js
//declaración de variables globales y librería de métodos para el manejo
//y control de las secuencias de usuario. el código para añadir datos
//a secuenciasRaw se inserta en cada petición (ajax o no) que crea datos simples.

//variable global: almacena el número de accion del usuario, usado en cada llamada ajax
//que genera datos simples en el servidor, y para ordenar y asegurar los datos en
//las secuenciasRaw
//var numAccion = 0;
//NOTA: en los métodos del controlador, se asigna -1 a numAccion si no se establece valor,
//para saber que la asignación de valor está fallando

//variable global: objeto JSON dónde se almacena en bruto la secuencia de usuario
//se envia al finalizar el test para comprobar la integridad de los DatoSimples
//almacenados en la BD durante la realización del test.
//contiene objetos del tipo:
//<numAccion> <datetime> <método llamado> <¿parámetros?>
//var datosRaw = {};

$(document).ready(function () {
    
    /*
    if (sessionStorage.getItem("numAccion") == null) {
        sessionStorage.setItem("numAccion", "0");
    }

    if (sessionStorage.getItem("datosRaw") == null) {
        var datosRaw = { "0": "primer-dato" };
        alert("secuencias controller working!");
        sessionStorage.setItem("datosRaw", JSON.stringify(datosRaw));
    }
    */
    
});
