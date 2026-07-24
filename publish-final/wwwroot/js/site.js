window.downloadFileFromStream = async function (fileName, contentStreamReference) {
    try {
        const arrayBuffer = await contentStreamReference.arrayBuffer();

        const blob = new Blob(
            [arrayBuffer],
            { type: "text/csv;charset=utf-8" }
        );

        const url = URL.createObjectURL(blob);
        const anchor = document.createElement("a");

        anchor.href = url;
        anchor.download = fileName;
        anchor.style.display = "none";

        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();

        setTimeout(function () {
            URL.revokeObjectURL(url);
        }, 1000);
    }
    catch (error) {
        console.error("Error descargando archivo:", error);
        throw error;
    }
};
