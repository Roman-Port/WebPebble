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