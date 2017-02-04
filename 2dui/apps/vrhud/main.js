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

	window.onload = function() {
		
		setInterval(function() {
		updateRoomURL();	
		},500)
		
	}