/**
 *
 * RPC Calls
 *
 * (C) Roland Schabenberger
 *
 */

var rpc = new function() {

	this.xmlHttp = null;
	
	this.initialize = function () {
	
		try {
			// Mozilla, Opera, Safari sowie Internet Explorer (ab v7)
			this.xmlHttp = new XMLHttpRequest();
		} catch (e) {
			try {
				// MS Internet Explorer (ab v6)
				this.xmlHttp  = new ActiveXObject("Microsoft.XMLHTTP");
			} catch (e) {
				try {
					// MS Internet Explorer (ab v5)
					this.xmlHttp  = new ActiveXObject("Msxml2.XMLHTTP");
				} catch (e) {
					this.xmlHttp  = null;
				}
			}
		}
	}
	
	this.getRequest = function (requestUrl) {
		
		try {
			this.xmlHttp.open('GET', requestUrl, false);
			this.xmlHttp.send(null);
			
			if (4 != this.xmlHttp.readyState) { // DONE ?
				return null;
			}
			
			if (200 != this.xmlHttp.status) {
				return null;
			}
			
			if ("" == this.xmlHttp.responseType || "text" == this.xmlHttp.responseType) {
				return this.xmlHttp.responseText;
			}
		} catch (e) {
			;
		}
		
		return null;
	}

	this.postRequest = function (cmd, params) {
		
		try {
			this.xmlHttp.open('POST', cmd, false);
			this.xmlHttp.send(params);
			
			if (4 != this.xmlHttp.readyState) { // DONE ?
				return null;
			}
			
			if (200 != this.xmlHttp.status) {
				return null;
			}

			if (undefined == this.xmlHttp.responseType ||
			    "" == this.xmlHttp.responseType ||
				"text" == this.xmlHttp.responseType) {
				return this.xmlHttp.responseText;
			} else {
				return this.xmlHttp.response;
			}
		} catch (e) {
			;
		}
		
		return null;
	}

	this.postAsyncRequest = function (cmd, params, handler) {
	
		var request = $.ajax({
			type: "POST",
			url: cmd,
			cache: false,
			processData : false,
			data: params,
			dataType: "text"
		});
		
		request.done(function( responseText, status, obj ) {
			handler(responseText);
		});
		
		request.fail(function() {
			handler(null);
		});
	}
	
}
