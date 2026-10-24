window.RetryAssurancePortal=Object.freeze({
  async copy(value){if(!navigator.clipboard?.writeText)return false;try{await navigator.clipboard.writeText(value);return true;}catch{return false;}},
  download(filename,value,mime){const url=URL.createObjectURL(new Blob([value],{type:mime})),anchor=document.createElement('a');anchor.href=url;anchor.download=filename.replace(/[^A-Za-z0-9._-]/g,'-');document.body.append(anchor);anchor.click();anchor.remove();setTimeout(()=>URL.revokeObjectURL(url),0);}
});
