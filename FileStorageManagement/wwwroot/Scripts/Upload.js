//"use strict";

let fileStorageUrl = 'https://localhost:7143/api';
let area = 'Cert';
let consumerId = 1;
//let bucket = '10025_40301';
let fileRefs = []; // Practically this should persist in the File storage consumer db

async function Download() {
   let fileRegistryId = prompt('Enter File fileRegistryId');
   if (fileRegistryId) {
      let fileName = prompt('Enter fileName'); // Ideally consumer has to kkep the file name in it's own db while uploading, and send it in the request while downloading file
      if (fileName) {
         let url = `${fileStorageUrl}/Files/${fileRegistryId}/fileName/${fileName}/download`;
         console.log(url);
         await DownloadURI(url, 'File');
      }
   }
}

async function DownloadURI(uri, name) {
   var link = document.createElement("a");
   //link.download = name;
   link.href = uri;
   document.body.appendChild(link);
   link.click();
   document.body.removeChild(link);
   delete link;
}

async function Delete() {
   let fileRegistryId = prompt('Enter File fileRegistryId to delete');

   let apiResponse = await DeleteFile(fileRegistryId);

   if (apiResponse.statusCode != 200) {
      alert(`Error code: ${apiResponse.statusCode}. ${apiResponse.message}`);
   }
   else {
      //Consumer should delete the file ref from it's own repo here
      alert("Done");
   }
}
async function DeleteFile(fileRegistryId) {
   if (fileRegistryId) {
      let url = `${fileStorageUrl}/Files/${fileRegistryId}/delete`;
      let response = await fetch(url, { method: "DELETE" });

      let apiResponse;
      if (response.status != 200) {
         console.log('Communication error!');
         console.log(response);
         //Need to put retry logic here in case of timeout error
         apiResponse.statusCode = -1; // -ve error codes denote non api errors
         apiResponse.message = 'Communication error!';
         return apiResponse;
      }
      //console.log(await response.json())
      apiResponse = await response.json();
      return apiResponse;
   }
}
async function Upload(fileBrowser, fileDetailsSpanId, progressSpanId) {
   var fileDetailsSpan = document.getElementById(fileDetailsSpanId);
   var progressSpan = document.getElementById(progressSpanId);

   let fileHash = await CalculateFileHash(fileBrowser, fileDetailsSpan, progressSpan);
   let response = await CheckFileExistsOnServer(fileHash);

   if (response.status == 200) {
      let fileExitsResponse = await response.json();

      console.log(fileExitsResponse);

      if (fileExitsResponse.statusCode == 3) { // File is not registered on server
         console.log('upload starting....');

         let uploadResponse = await UploadFileToServer2(fileBrowser, fileHash, "", 0, fileDetailsSpan, progressSpan);
         console.log(uploadResponse);

         if (uploadResponse.statusCode == 200) {
            progressSpan.innerHTML = `File upload completed. Storage FileRegistryId: ${uploadResponse.result.fileRegistryId}`;

            let fileRef = {
               "FileRegistryId": uploadResponse.result.fileRegistryId,
               "FileName": fileBrowser.files[0].name,
               "UploadDate": new Date()
            }

            fileRefs.push(fileRef); // Practically this should be in a db
         }
         else {
            console.log('Error uploading new file-->');
            console.log(uploadResponse);
         }
      }
      else if (fileExitsResponse.statusCode == 2) { // File is partially uploaded on server
         console.log('File is partially uploaded on server');

         let partialUploadResult = fileExitsResponse.result;
         let fileGuid = partialUploadResult.fileGuid;
         let fileReadStartAtPosition = partialUploadResult.fileSize;

         console.log(`${fileReadStartAtPosition} bytes already uploaded`);
         console.log('upload the remaining part....');

         let uploadResponse = await UploadFileToServer2(fileBrowser, fileHash, fileGuid, fileReadStartAtPosition, fileDetailsSpan, progressSpan);
         console.log(uploadResponse);

         if (uploadResponse.statusCode == 200) {
            progressSpan.innerHTML = `File upload completed. Storage fileRegistryId: ${uploadResponse.result.fileRegistryId}`;
         }
         else {
            console.log('Error uploading reamining part of the file-->');
            console.log(uploadResponse);
         }
      }
      else if (fileExitsResponse.statusCode == 1) {
         //if File already exists in the registry then consumer should check if the is already referenced by the consumer
         // if Not already referenced by the current consumer, then increment ref count for existing file on file storage server
         progressSpan.innerHTML = `File already exists. Storage fileRegistryId: ${fileExitsResponse.result}`;
         console.log(fileRefs);
         console.log(fileExitsResponse.result);
         console.log(fileBrowser.files[0].name);
         let fileRef = fileRefs.find(x => x.FileRegistryId == fileExitsResponse.result && x.FileName == fileBrowser.files[0].name);
         if (fileRef == undefined) { //Not already referenced by the current consumer, hence increment ref count for existing file on file storage server
            console.log('Not already referenced by the current consumer');

            let url = `${fileStorageUrl}/Files/${fileExitsResponse.result}/IncrementRefCount`;
            let body = { "consumerId": consumerId };
            let response = await FetchAsync(url, 'PUT', { "Content-Type": "application/json" }, body);
            console.log(response);
            if (response.status == 200) {
               let fileRef = {
                  "FileRegistryId": fileExitsResponse.result,
                  "FileName": fileBrowser.files[0].name,
                  "UploadDate": new Date()
               }

               fileRefs.push(fileRef); // Practically this should be in a db
            }
         }
         else {
            console.log('File already is in use in current app');
         }
      }
      else {
         alert(fileExitsResponse.message);
      }
   }
   else {
      console.log(response);
   }

   fileBrowser.value = null;
}

async function FetchAsync(url, method, headers, data) {
   // Default options are marked with *
   const response = await fetch(url, {
      method: method, // *GET, POST, PUT, DELETE, etc.
      //mode: "cors", // no-cors, *cors, same-origin
      //cache: "no-cache", // *default, no-cache, reload, force-cache, only-if-cached
      //credentials: "same-origin", // include, *same-origin, omit
      headers: headers,
      //redirect: "follow", // manual, *follow, error
      //referrerPolicy: "no-referrer", // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
      body: JSON.stringify(data), // body data type must match "Content-Type" header
   });

   return response; // parses JSON response into native JavaScript objects
}

async function PostData(url, _headers, data) {
   // Default options are marked with *
   const response = await fetch(url, {
      method: "POST", // *GET, POST, PUT, DELETE, etc.
      //mode: "cors", // no-cors, *cors, same-origin
      //cache: "no-cache", // *default, no-cache, reload, force-cache, only-if-cached
      //credentials: "same-origin", // include, *same-origin, omit
      headers: _headers,
      //redirect: "follow", // manual, *follow, error
      //referrerPolicy: "no-referrer", // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
      body: JSON.stringify(data), // body data type must match "Content-Type" header
   });

   //console.log(response);
   //console.log('---001');
   return response; // parses JSON response into native JavaScript objects
}

async function CheckFileExistsOnServer(fileHash, area, bucket, fileName) {
   let url = `${fileStorageUrl}/Files/hash/${fileHash}/exists`;
   let response = await fetch(url);

   return response;
}

async function UploadFileToServer2(fileBrowser, fileHash, fileGuid = "", start = 0, fileDetailsSpan, progressSpan) {
   let lblTimeRemaining = document.getElementById('lblTimeRemaining');
   let overAllProgressBar = document.getElementById('overAllProgress');

   let url = `${fileStorageUrl}/Files/upload`;

   let apiResponse = {};

   if (fileBrowser.files.length > 0) {
      const fileName = fileBrowser.files[0].name;

      const file = fileBrowser.files[0];
      const fileSize = file.size;

      let headers = {
         //"contentType": "application/octet-stream",
         "fileSize": fileSize,
         "fileGuid": fileGuid,
         "fileHash": fileHash,
         "area": area,
         "consumerId": consumerId,
         "hasContent": true,
         "eof": false
      };

      fileDetailsSpan.innerHTML = `${fileName} (${((fileSize / 1024) / 1024).toFixed(2)} mb)`;

      let initialChunkSize = 1024 * 1024 * 5;
      let chunkSizeOnwards = 1024 * 1024 * 50;
      let chunkSize = initialChunkSize; // Size of each chunk in bytes

      if (fileSize < chunkSize) {
         chunkSize = fileSize;
      }

      let percentComplete = Math.round((start / fileSize) * 100);
      overAllProgressBar.value = percentComplete;

      progressSpan.innerHTML = `Uploading file: ${percentComplete}%`;
      //uploadProgress.value = percentComplete;

      let response;

      if (start < fileSize) {
         let startTime = new Date();

         while (start < fileSize) {
            const chunk = file.slice(start, start + chunkSize);
            let result = await readChunkAsBinary(chunk);

            let view = new Uint8Array(result);

            if (start + chunkSize >= fileSize) {
               headers.fileSize = fileSize;
               headers.eof = true;
            }

            var blob = new Blob([view.buffer], { type: 'application/octet-stream' });
            response = await SendDataToServerAsync2(url, blob, headers);

            if (response.status != 200) {
               console.log('Communication error!');
               console.log(response);
               //Need to put retry logic here in case of timeout error
               apiResponse.statusCode = -1; // -ve error codes denote non api errors
               apiResponse.message = 'Communication error!';
               return apiResponse;
            }

            response = JSON.parse(response.response);

            apiResponse = response;

            if (apiResponse.statusCode != 200) {
               alert(`Error code: ${apiResponse.statusCode}. ${apiResponse.message}`);

               return apiResponse;
            }

            headers.fileGuid = apiResponse.result.fileGuid;

            start += chunkSize;
            if (start > fileSize) {
               start = fileSize;
            }

            let endTime = new Date();
            calculateApproxTransferTime(startTime, endTime, fileSize, start, lblTimeRemaining);

            chunkSize = chunkSizeOnwards;

            percentComplete = Math.round((start / fileSize) * 100);
            overAllProgressBar.value = percentComplete;
            progressSpan.innerHTML = `Uploading file: ${percentComplete}%`;
         }
      }
      else {
         headers.hasContent = false;
         headers.fileSize = fileSize;
         headers.eof = true;

         var blob = new Blob([], { type: 'application/octet-stream' });
         response = await SendDataToServerAsync2(url, blob, headers);

         if (response.status != 200) {
            console.log('Communication error!');
            console.log(response);
            //Need to put retry logic here in case of timeout error
            apiResponse.statusCode = -1; // -ve error codes denote non api errors
            apiResponse.message = 'Communication error!';
            return apiResponse;
         }

         response = JSON.parse(response.response);

         apiResponse = response;

         if (apiResponse.statusCode != 200) {
            alert(`Error code: ${apiResponse.statusCode}. ${apiResponse.message}`);

            return apiResponse;
         }

         progressSpan.innerHTML = `Uploading file: ${Math.round((start / fileSize) * 100)}%`;
      }

   }

   return apiResponse;
}

function calculateApproxTransferTime(startTime, endTime, fileSize, bytesTransferredSoFar, lblTimeRemaining) {
   let ms = (endTime - startTime);
   let remainingBytes = fileSize - bytesTransferredSoFar;
   let estimatedSecondsRemaining = parseInt(((ms / bytesTransferredSoFar) * remainingBytes) / 1000);
   let h = parseInt(estimatedSecondsRemaining / 3600);
   let seconds = estimatedSecondsRemaining % 3600;
   let m = parseInt(seconds / 60);
   seconds = seconds % 60;

   let timeMsg = `${h}h:${m}m:${seconds}s`;
   lblTimeRemaining.innerHTML = `Estimated time remaining: ${timeMsg}`;
}

async function SendDataToServerAsync2(url, file, headers) {
   uploadProgress.value = 0;

   const formData = new FormData();
   formData.append('file', file);

   const xhr = new XMLHttpRequest();
   xhr.open('POST', url, true);
   //xhr.setRequestHeader("Content-Type", headers.contentType);
   //xhr.setRequestHeader("fileName", headers.fileName);
   xhr.setRequestHeader("fileGuid", headers.fileGuid);
   xhr.setRequestHeader("fileHash", headers.fileHash);
   xhr.setRequestHeader("fileSize", headers.fileSize);
   xhr.setRequestHeader("area", headers.area);
   xhr.setRequestHeader("consumerId", headers.consumerId);
   xhr.setRequestHeader("hasContent", headers.hasContent);
   xhr.setRequestHeader("eof", headers.eof);

   xhr.upload.onprogress = function (event) {
      if (event.lengthComputable) {
         const percentComplete = (event.loaded / event.total) * 100;
         uploadProgress.value = percentComplete;
         //console.log(percentComplete);
      }
   };

   return new Promise((resolve, reject) => {
      xhr.onload = function () {
         resolve(xhr);
      };

      xhr.onerror = function () {
         resolve(xhr);
      };

      xhr.send(formData);
   });
}

async function CalculateFileHash(fileBrowser, fileDetailsSpan, progressSpan) {
   if (fileBrowser.files.length > 0) {
      var fileName = fileBrowser.files[0].name;

      const file = fileBrowser.files[0];
      const fileSize = file.size;
      fileDetailsSpan.innerHTML = `${fileName} (${((fileSize / 1024) / 1024).toFixed(2)} mb)`;
      let start = 0;
      let chunkSize = 1024 * 1024 * 20; // Size of each chunk in bytes
      if (fileSize < chunkSize) {
         chunkSize = fileSize;
      }

      let sha256 = CryptoJS.algo.SHA256.create();

      while (start < fileSize) {
         const chunk = file.slice(start, start + chunkSize);
         let result = await readChunkAsBinary(chunk);
         const chunkWordArray = CryptoJS.lib.WordArray.create(result);

         sha256.update(chunkWordArray);

         start += chunkSize;

         if (start > fileSize) {
            start = fileSize;
         }
         //await sleep(1000);
         progressSpan.innerHTML = `Calculating file hash: ${Math.round((start / fileSize) * 100)}%`;
      }

      const hash = sha256.finalize();
      const hashHex = hash.toString(CryptoJS.enc.Hex);
      return hashHex;
   }
   else {
      fileDetailsSpan.innerHTML = '(No file selected)';
   }
}

async function ReadFileAsText(fileBrowser, fileDetailsSpanId, progressSpanId) {
   var fileDetailsSpan = document.getElementById(fileDetailsSpanId);
   var progressSpanId = document.getElementById(progressSpanId);
   if (fileBrowser.files.length > 0) {
      var fileName = fileBrowser.files[0].name;


      const file = fileBrowser.files[0];
      const fileSize = file.size;
      fileDetailsSpan.innerHTML = `${fileName} (${((fileSize / 1024) / 1024).toFixed(2)} mb)`;
      let start = 0;
      let chunkSize = 1024 * 5; // Size of each chunk in bytes
      if (fileSize < chunkSize) {
         chunkSize = fileSize;
      }

      while (start < fileSize) {
         const chunk = file.slice(start, start + chunkSize);
         let result = await readChunkAsText(chunk);
         console.log(result);
         start += chunkSize;

         //await sleep(1000);
         progressSpanId.innerHTML = `${Math.round((start / fileSize) * 100)}%`;
      }
   }
   else {
      csvFile = null;
      targetLabel.innerHTML = '(No file selected)';
      ol.innerHTML = '';
   }
}

async function readChunkAsText(chunk) {
   const reader = new FileReader();

   return new Promise((resolve, reject) => {
      reader.onload = (e) => {
         resolve(e.target.result);
      };

      reader.onerror = (e) => {
         reject(new Error("Failed to read file"));
      };

      reader.readAsText(chunk);
   });
}

async function readChunkAsBinary(chunk) {
   const reader = new FileReader();

   return new Promise((resolve, reject) => {
      reader.onload = (e) => {
         resolve(e.target.result);
      };

      reader.onerror = (e) => {
         reject(new Error("Failed to read file"));
      };

      reader.readAsArrayBuffer(chunk);
   });
}

function sleep(ms) {
   return new Promise(resolve => setTimeout(resolve, ms));
}
