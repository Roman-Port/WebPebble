var accounts = {};

accounts.loggedIn = false;
accounts.userData = {};

accounts.init = function(signedInCallback, loggedOutCallback) {
    standard.serverRequest("https://api.webpebble.get-rpws.com/users/@me/", function(data) {
        accounts.loggedIn = true;
        accounts.userData = data;
        signedInCallback();
    }, null, true, "GET", null, 2000, function(errorData) {
        //Known server error. Likely to be logged out.
        if(errorData.code == 0) {
            //This user isn't logged in. 
            loggedOutCallback();
        }
        else {
            //Unknown error.
            project.showDialog("Unknown Server Error", errorData.message, ["Reload"], [function () { window.location.reload(); }]);
        }
    });
};