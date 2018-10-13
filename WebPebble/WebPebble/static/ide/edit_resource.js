var edit_resource = {};

edit_resource.onSelect = function () {
    //Selected to start.

    //Make sure we can see the selected type.
    edit_resource.onTypeChange('bitmap');
}

edit_resource.onTypeChange = function (type) {
    //Hide all of the special types.
    var typeDivs = document.getElementsByClassName('typeselect_hidden');
    for (var i = 0; i < typeDivs.length; i += 1) {
        typeDivs[i].style.display = "none";
    }
    //If it exists, show the special div for this.
    var specialDiv = document.getElementById('typeselect_template_' + type);
    if (specialDiv != null) {
        specialDiv.style.display = "block";
    }
}

edit_resource.onFontSizeChange = function (value) {
    //Fill in the "identifier" box.
    document.getElementById('typeselect_font_id').innerText = document.getElementById('addresrc_entry_id').value + "_" + value;
}