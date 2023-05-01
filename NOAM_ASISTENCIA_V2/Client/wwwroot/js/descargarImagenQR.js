function descargarImagenQR(fileName, content) {
    let link = document.createElement('a');

    link.download = fileName;
    link.href = 'data:image/png;base64,' + encodeURIComponent(content);

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}