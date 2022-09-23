function TestsCors() {
    var tokenJWT = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6ImpvYW9AZ21haWwuY29tIiwic3ViIjoiMzMxZGFiOTgtMDk5Ni00Njc3LTk0MDctZTYwYTc3ZjFmZmY3IiwiZXhwIjoxNjYzOTU5Mzk4fQ.5n1u91HR3aX8METQ8SCj6ecaY9uA5D--kckwttTGjpw";
    var servico = "https://localhost:44313/api/mensagem/ecf58ab0-7d13-496b-a34d-7f0e3f78aa37/841b66ab-7d0a-4bf8-b62b-0abba68a27ad";
    $("#resultado").html("---Solicitando---");
    $.ajax({
        url: servico,
        method: "GET",
        crossDomain: true,
        headers: {"Accept": "application/json"},
        beforeSend: function (xhr) {
            xhr.setRequestHeader("Authorization", "Bearer " + tokenJWT);
        },
        success: function (data, status, xhr) {
            $("#resultado").html(data);
            console.info(data)
        }
    });
}