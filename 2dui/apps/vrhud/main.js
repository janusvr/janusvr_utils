	function goToRoom(url) {
		
	var keyPressed = event.keyCode || event.which;

	if(keyPressed==13)
		{
			window.janus.launchurl(url,1);
		}
	}
	
	
	var prevURL;
	function updateRoomURL(){
		
		if ((prevURL != window.janus.currenturl()) || (document.getElementById("myurl").value.length == 0))
		{
			prevURL = window.janus.currenturl();
			document.getElementById("myurl").value = prevURL;
		}
	
	
	}
	
	function updateProgressBar() {

		var progbar = document.getElementById("vrurlcontainer");
		var progressx = (window.janus.roomprogress())*200;

		document.getElementById("vrurlcontainer").style.backgroundSize = ""+progressx+"% 100%";

		
	}

	window.onload = function() {
		
		setInterval(function() {
		updateProgressBar();
		updateRoomURL();	
		},500)
		
	}