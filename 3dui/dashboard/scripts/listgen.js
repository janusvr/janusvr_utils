	function randomNumber(seed) {
	
	var origseed = seed;
		if (typeof seed == "string")
		{
		seed = origseed.length*origseed.charCodeAt(1);
		}
	
    var x = Math.sin(seed++) * 10000;

    return x - Math.floor(x);
	
	}
	
	
	
  	function extractDomain(url) {
  	    
		var domain;
		if (url != null)
		{
		//find & remove protocol (http, ftp, etc.) and get domain
		if (url.indexOf("://") > -1) {
			domain = url.split('/')[2];
		}
		else {
			domain = url.split('/')[0];
		}

		//find & remove port number
		domain = domain.split(':')[0];
		}
		else
		{
		domain = "URL Not Found";	
		}
		return domain;
}
  
  
  	function strip(html)
	{
		var tmp = document.createElement("DIV");
		tmp.innerHTML = html;
		return tmp.textContent || tmp.innerText;
	}
	
  
  
  
 function returnMainArray(arraytype) {

	if (arraytype == "partymode")
	{
		xx = parent.window.janus.partymodedata;	
	}
	else if (arraytype == "bookmarks")
	{
		xx = parent.window.janus.bookmarks;		
	}
	else if (arraytype == "workspaces")
	{
		
		xx = parent.window.janus.workspaces;
	}
	else if (arraytype == "popular")
	{	
		xx = parent.window.janus.populardata;

	
	}	
	
	return xx;
	
} 
  
	function trimString(text,mylength) {
	

	var trimmedString = text.length > mylength ? 
                    text.substring(0, mylength - 3) + "..." : 
                    text;	 
					
	return trimmedString;
	
	}
	
	
function populatePartyObject(){
    parent.window.janus.updatepartymodedata() 
    
}	
	
	
function populatePopularObject(){

    parent.window.janus.updatepopulardata("?orderBy=weight&desc=true&limit=50") 

}	
		
	
	
        function generateList(arraytype) {
		
            var containertopopulate = document.getElementById("cardholder");
			var currentscroll = window.pageYOffset;
            containertopopulate.innerHTML = ""
            var mainarray = returnMainArray(arraytype);
			
              	if (arraytype == "partymode")
            		{
						if ((mainarray.length < 1))
						{
							
							document.getElementById("mainbody").style.backgroundImage = "url('../../backgrounds/parties.png')";
							
							
						}
						else
						{
							document.getElementById("mainbody").style.backgroundImage = "";
						}
					
					
					}
					
            
            
            for (var i=0;i<mainarray.length;i++)
            {
                

                
                
                
            		if ((arraytype == "partymode"))
            		{
            			if (mainarray[i].name == null || mainarray[i].name == "" )
            			{
            				
            
            				sitename = extractDomain(mainarray[i].url);
            
            			
            			
            			}
            			else
            			{
            				sitename = mainarray[i].name
            				sitename = trimString(sitename,32);
            			}
            		}
            		else if ((arraytype == "bookmarks"))
            		{
            			if (mainarray[i].title == null || mainarray[i].title == "" )
            			{
            				
            
            				sitename = extractDomain(mainarray[i].url);
                            
            			
            			
            			}
            			else
            			{
            				sitename = mainarray[i].title
            				sitename = trimString(sitename,32);
            			}			
            		}
            		else if (arraytype == "workspaces")
            		{
            			sitename = "";
            		}
					else if ((arraytype == "popular"))
            		{
            			if (mainarray[i].roomName == null || mainarray[i].roomName == "" )
            			{
            				
            
            				sitename = extractDomain(mainarray[i].roomUrl);
            
            			
            			
            			}
            			else
            			{
            				sitename = mainarray[i].roomName
            				sitename = trimString(sitename,32);
            			}
            		}					
                
                
                var partymodeprefix="";
                var partymodestring="";
                
                if (arraytype == "partymode")
                {
                     partymodeprefix = "<b>"
                     partymodestring = "</b><br> with "+trimString(mainarray[i].userId,24);
                }
                
                
                
                
                //generate elements

          
                var dashcard=document.createElement("div");
                dashcard.className = "dashcard"
                
                
					if ((arraytype == "partymode"))
					{
						dashcard.setAttribute("style","-webkit-filter:hue-rotate(" + (randomNumber(strip(mainarray[i].userId))*360) + "deg) brightness(0.75);visibility: visible !important;")	
					}
					else if ((arraytype == "bookmarks") || (arraytype == "workspaces"))
					{
						dashcard.setAttribute("style","background:linear-gradient(rgba(0, 0, 0, 0.2),rgba(0, 0, 0, 0.2)), url('" + mainarray[i].thumbnail + "') no-repeat scroll center;visibility: visible !important;background-size:cover;")
						
					}
					else if ((arraytype == "popular"))
					{
						
						dashcard.style.backgroundImage = "url('../../thumbs/popular.png')"
				
						
					}					
                
                
                containertopopulate.appendChild(dashcard)
                
            
              
                if (sitename == "")
                {
                sitename = "Untitled"
                }
                
                var dashtitle=document.createElement("div");
                dashtitle.innerHTML = (partymodeprefix+strip(sitename)+partymodestring).toUpperCase();  
                dashtitle.className = "dashtitle"
                dashcard.appendChild(dashtitle);   
                
          
                
                if (dashtitle.offsetHeight >54 )
                {
                dashtitle.style.bottom = "15px";
				
                }
       
       
                var hiddenurlcontainer=document.createElement("div");
				
				if (arraytype == "popular")
				{
				hiddenurlcontainer.innerHTML = mainarray[i].roomUrl;
				}
				else
				{
                hiddenurlcontainer.innerHTML = mainarray[i].url;   
                }
				hiddenurlcontainer.className = "hiddenurlcontainer";
                hiddenurlcontainer.id = function(arg) {
								return "myurl"+[arg];
										}(i);
				
           								
												
                dashcard.appendChild(hiddenurlcontainer);    
           
                              	dashcard.onclick =  function(arg) {
								return function() {
									parent.window.janus.launchurl(document.getElementById("myurl"+[arg]).innerHTML,0)
										}
										}(i);
								
           
	
            window.scrollTo(0, currentscroll);        
            }
		   
            
        }
  
  
        
