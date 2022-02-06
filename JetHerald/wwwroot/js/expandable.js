$(function () {
    $(".showable-container > .showable")
        .css("display", "none")
        .removeClass("hide");
    $(".showable-container > .show-button").click(function () {
        $(this).parent().children(".showable").toggle();
    });
});