function ShowMenu() {
    document.getElementsByClassName('text')[0].className = "text text-active";
    document.getElementsByClassName('browser')[0].className = "browser browser-active";
}








//First, check if we're logged in.
var xmlhttp = new XMLHttpRequest();
xmlhttp.onreadystatechange = function () {
    if (this.readyState == 4 && this.status == 200) {
        //Signed in
        //Show the correct button
        document.getElementById('signedin').style.display = "inline-block";
        ShowMenu();
    } else if (this.readyState == 4) {
        //Not signed in
        //Show the correct button
        document.getElementById('signin').style.display = "inline-block";
        ShowMenu();
    }
}

xmlhttp.open("GET", "https://api.webpebble.get-rpws.com/users/@me/", true);
xmlhttp.withCredentials = true;
xmlhttp.send(null);