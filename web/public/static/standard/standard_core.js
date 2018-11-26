var standard = {};

standard.serverRequest = function (url, run_callback, fail_callback, isJson, type, body, timeout, knownServerErrorCallback) {
    //This is the main server request function. Please change all other ones to use this.
    if (isJson == null) { isJson = true; }

    if (type == null) { type = "GET"; }

    if (timeout == null) { timeout = 10000; }

    if (fail_callback == null) {
        fail_callback = function () {
            project.showDialog("Error", "Failed to connect. Please check your internet and try again later.", ["Retry"], [function () { project.anyServerRequest(url, run_callback, fail_callback, isJson, type, body, timeout);  }]);
        }
    }
    var xmlhttp = new XMLHttpRequest();

    xmlhttp.timeout = timeout;

    xmlhttp.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            if (isJson) {
                //This is JSON.
                //This is most likely to be valid, but check for errors.
                var JSON_Data;
                try {
                    JSON_Data = JSON.parse(this.responseText);
                } catch (e) {
                    fail_callback("JSON Parse Error", true);
                    return;
                }
                //Return it
                run_callback(JSON_Data);
            } else {
                //Just return it
                run_callback(this.responseText);
            }
        } else if (this.readyState == 4) {
            //Parse the response and display the error
            var errorData = JSON.parse(this.responseText);
            knownServerErrorCallback(errorData);
        }
    }

    xmlhttp.ontimeout = function () {
        fail_callback("No Connection", false);
    }

    xmlhttp.onerror = function () {
        fail_callback("No Connection", false);
    }

    xmlhttp.onabort = function () {
        fail_callback("Abort", false);
    }
    //Todo: Add timeout error.
    xmlhttp.open(type, url, true);
    xmlhttp.withCredentials = true;
    xmlhttp.send(body);
};