"use strict";
let messageContainer = null;

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

connection.on("ReceiveMessage", function (cmd, message) {
   if (messageContainer == null || cmd.startsWith('--createNewMessageContainer')) {
      messageContainer = document.createElement("li");
      document.getElementById("messagesList").appendChild(messageContainer);
   }
   messageContainer.textContent = `${message}`;
});

connection.start().then(function () {
   console.log('SignalR started');
}).catch(function (err) {
   return console.error(err.toString());
});