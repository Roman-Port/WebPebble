function AddProject(img, name, url) {
    var e = document.createElement('div');
    e.className = "project";

    var top = document.createElement('div');
    top.className = "p_top";
    e.appendChild(top);

    var text = document.createElement('div');
    text.className = "p_text";
    text.innerText = name;
    e.appendChild(text);

    e.x_url = url;
    e.addEventListener('click', function() {
        window.location = this.x_url;
    });

    document.getElementById('projects').appendChild(e);
    return e;
}


function CreateProjBtn() {
    if (confirm("IMPORTANT! Your project may be wiped at any time, without warning, before the release of WebPebble in late 2018. YOU ARE USING ALPHA SOFTWARE. By creating a project now, you agree that RPWS or WebPebble have NO GUARANTEE OF STORAGE OR WARRANTY.")) {
        standard.displayForm("Create Project", [
            {"title":"Name", "type":"text"},
            {"title":"Type", "type":"select", "options":[
                {"title":"Watchapp", "value":"false"},
                {"title":"Watchface", "value":"true"}
            ]}
        ], function(data){
            standard.showLoader("Creating Project...");
            standard.serverRequest("https://api.webpebble.get-rpws.com/create?title="+encodeURIComponent(data[0])+"&watchface="+encodeURIComponent(data[1]), function(data) {
                if(data.ok) {
                    //Redirect here.
                    window.location = data.data;
                } else {
                    //Error.
                    standard.showDialog("Failed to Create Project", data.data, ["Retry"], [CreateProjBtn])
                }
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
            }, true);


        }, function(){});
    }
}

accounts.init(function() {
    //Show projects
    for(var i = 0; i<accounts.userData.projects.length; i+=1) {
        var d = accounts.userData.projects[i];
        if(d.name != null) {
            AddProject(null, d.name, d.href);
        }
    }
}, function() {
    //Not logged in.
    window.location = "/login";
})