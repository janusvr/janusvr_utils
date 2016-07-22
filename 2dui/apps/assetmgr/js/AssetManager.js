function log(text) {
  // Disable debug logging ^-^
  //$('#logger').text(text + '\n' + $('#logger').text());
}

function FileEntry(hash, name, type) {
  return {
    hash : hash,
    filename : name,
    filetype : type
  }
}

function AssetManager(options) {
  log('Starting inventory');


  this.options = options;
  this.domElement = options.target;
  this.domElement.addClass('folder');
  this.cache = {};

  // Click and dragging
  this.domElement.on('click', '.entry-display', function(ev) {
    var $this = $(ev.currentTarget);
  }.bind(this));


  var dragState = {
    el : null,
    time : 0,
    startX : 0,
    startY : 0,
    isDragging : false
  };

  $(document).on('mousedown', '.entry-display', function(ev) {
    var $this = $(ev.currentTarget);

    var entry = $this.data('entry');


    dragState = {
      el : $this,
      time : new Date().getTime(),
      startX : ev.pageX,
      startY : ev.pageY,
      isDragging : false
    };
    console.log('Drag state set');
  }.bind(this));

  $(document).on('mouseup', function(ev) {



    if (dragState.el) {
      dragState.el.css('border', 'none');
    }

    dragState.time = 0;
    dragState.el = null;
    dragState.isDragging = false;

    console.log('Drag state cleared');
  }.bind(this));

  $(document).on('mousemove', function(ev) {
    if (!dragState.el)
      return;

    if (!dragState.isDragging) {
      var diffX = Math.pow(ev.pageX - dragState.startX, 2);
      var diffY = Math.pow(ev.pageY - dragState.startY, 2);
      var diff = Math.sqrt(diffX + diffY);

      if (diff > 4) {
        dragState.isDragging = true;
      } else {
        return;
      }
    }

    dragState.el.css('border', '5px solid red');

  }.bind(this));

  $(document).on('keydown', function(ev) {
    if (ev.which == 46) {
      this.deleteEntries($('.active').parent());
    }
  }.bind(this));
  /* ---------------------
      Cancel renaming if we click on anything except for
      the renaming input.
     --------------------- */
  $(document).on('click', function(ev) {
    if (!$(ev.target).parent().hasClass('renaming'))
      this.cancelRenamingFile();
  }.bind(this))

  /* ---------------------
      Handle keypresses for canceling/saving renaming
     --------------------- */
  this.domElement.on('keydown', '.renaming input', function(ev) {
    ev.stopPropagation();
    if (ev.which == 13) {
      this.finishRenameFile();
    } else if (ev.which == 27) {
      this.cancelRenamingFile();
    }
  }.bind(this));
  /* ---------------------
    Double click functionality
      For expanding folders, opening previews, and renaming
     --------------------- */
  this.domElement.on('click', '.entry-display', function(ev) {
    var $this = $(ev.currentTarget);

    var now = new Date().getTime();
    var timeDiff = now - $this.data('lastclick');

    if (timeDiff < 300) {
      var entry = $this.data('entry');

      if (entry.data('filetype') == 'folder') {
        this.toggleFolder(entry);
      } else {
        this.previewAsset(entry);
      }

      $this.data('lastclick', 0);
    } else if ($this.hasClass('active') && !$this.hasClass('renaming') && timeDiff > 500) {

      var entry = $this.data('entry');
      this.startRenameFile(entry);


    } else {
      var entry = $this.data('entry');
      this.selectEntry(entry);
      $this.data('lastclick', now);
    }
  }.bind(this));

  // Expander mouse handling
  this.domElement.on('click', '.expander', function(ev) {
    var $this = $(ev.currentTarget);

    var folder = $this.data('entry');
    this.toggleFolder(folder);
  }.bind(this))

  this.loadInventory(function() {
    this.parseInventory(this.inventory);
  }.bind(this));

  this.initUploader();
  //this.refreshFolder();

  $('#create-folder').on('click', function() {
    this.addFolder('New Folder', this.getActiveFolder());
  }.bind(this));

  log('Herp derping');
}

AssetManager.prototype.saveInventory = function(cb) {
  cb = cb || function(){};

  ipfs.addJson(this.inventory, function(err, hash) {
    if (err) {
      alert('Failed to save inventory! ' + err);
      return;
    }

    this.invHash = hash;
    setCookie('invhash2', this.invHash, 10000);
    cb();
  })
}

AssetManager.prototype.loadInventory = function(cb) {
  this.invHash = getCookie('invhash2');
  log(this.invHash);
  if (!this.invHash) {
    log('No inventory exists');
    this.inventory = this.generateInventory();

    this.saveInventory(function() {
      this.loadInventory(cb);
    }.bind(this));
    return;
  }


  ipfs.catJson(this.invHash, function(err, data) {
    log('Inventory loaded!');
    this.inventory = data;
    log(JSON.stringify(data));
    this.nodeMap = new Map();
    this.nodeMap.set(this.inventory, this.domElement);
    this.domElement.data('invNode', this.inventory);
    cb();
  }.bind(this))
}

AssetManager.prototype.generateInventory = function() {
  log('Creating new inventory');

  /* Create default inventory, right now it's just empty,
    but can later have hardcoded hashes if we with


    E.g:
    */
    /*var inv = {
      children:[
        {
          file : FileEntry('aosjef89e65uhe56u23jr', 'File name', 'png')
        },
        {
          file : FileEntry('fasdfaujr9utrq9t84rf', 'Folder name', 'folder'),
          children : [{
            file : FileEntry('at49ug93ku56b3465gsd', 'readme', 'txt')
          }]
        }
      ]
    };*/



  var inv = {children: []};

  return inv;
}

AssetManager.prototype.initUploader = function() {
  var fileSel = $('<input/>');

  fileSel.attr({
    type: 'file'
  })
  .css({
    display: 'none'
  })

  var uploadBtn = $('<input/>');
  uploadBtn.attr({
    type: 'button',
    value: 'Upload File'
  })

  uploadBtn.on('click', function() {
    fileSel.click();
  });

  $('#uploader-target')
    .append(fileSel)
    .append(uploadBtn);

  fileSel.on('change', function(ev) {
    this.uploadFile(ev.target.files[0], function() {
      fileSel[0].value = null;
    }.bind(this));
  }.bind(this));
}

AssetManager.prototype.uploadFile = function(file, cb) {

  var toks = file.name.split('.');

  var fileType = toks.pop()
  var fileName = toks.join('.');

  var fr = new FileReader();
  fr.onload = function(ev) {
    var arrayBuffer = ev.target.result;

    // Convert blob to buffer.
    //ipfs.Buffer(blob, function(buffer) {
      // Add the buffer to ipfs
      ipfs.add(arrayBuffer, function(err, hash) {
        if (err)
          console.log('Error', err);

        var folderEl = this.getActiveFolder();
        var folderJsonNode = folderEl.data('invNode');

        var childNode = {
          file : new FileEntry(hash, fileName, fileType)
        };

        folderJsonNode.children.push(childNode);

        this.addToFolder(folderEl, childNode)
        this.saveInventory();
        cb();
      }.bind(this));
    //}.bind(this));
  }.bind(this);
  fr.readAsArrayBuffer(file);
}

AssetManager.prototype.addFolder = function(name, folderEl) {
  var folderJsonNode = folderEl.data('invNode');

  var childNode = {
    file : new FileEntry('', name, 'folder'),
    children : []
  };

  folderJsonNode.children.push(childNode);

  this.addToFolder(folderEl, childNode)
  this.saveInventory();
}

AssetManager.prototype.getActiveFolder = function() {
  var active = this.domElement.find('.active').eq(0);

  if (active.length == 0)
    return this.domElement;

  if (active.hasClass('folder'))
    return active;

  return active.closest('.folder');
}

AssetManager.prototype.previewAsset = function(entry) {
  parent.postMessage({
    cmd : 'toggleWindow',
    callerid : 'null',
    width : 400,
    height : 400,
    title : 'Asset Previewer<div style="font-size: 8px; position: absolute; opacity: 0">' + entry.data('uuid') + '<div>',
    page : 'apps/assetmgr/preview.html?uuid=' + entry.data('uuid') + '&filetype=' + entry.data('filetype'),
    spawnx : '10vw',
    spawny : '20vh'
  }, '*');
}

AssetManager.prototype.startRenameFile = function(entry) {
  this.cancelRenamingFile();

  var display = entry.children('.entry-display');
  display.addClass('renaming');
  var input = $('<input />');
  input.val(display.children('.filename').text());
  display.append(input);
  input.focus();
}

AssetManager.prototype.finishRenameFile = function() {
  var renaming = $('.renaming');
  if (renaming.length == 0)
    return;

  var name = renaming.find('input').val()
  renaming.children('.filename').text(name);

  var jsonNode = renaming.parent('li').data('invNode');
  jsonNode.file.filename = name;

  this.cancelRenamingFile();
  this.saveInventory();
}

AssetManager.prototype.cancelRenamingFile = function() {
  var renaming = $('.renaming');

  if (renaming.length == 0)
    return;

  renaming.removeClass('renaming');
  renaming.find('input').remove();
}

AssetManager.prototype.deleteEntries = function(entries) {
  /* TODO: this will likely cause problems if you have a parent
    and child selected and the parent is deleted before the child. I dunno.
    YOLO, I'll fix it later when multi select is possible */

  entries.each(function() {
    var $this = $(this);
    var jsonNode = $this.data('invNode');
    var parent = $this.parent().closest('.folder');
    var parentJsonNode = parent.data('invNode');

    var i = parentJsonNode.children.indexOf(jsonNode);
    parentJsonNode.children.splice(i,1);
  });

  entries.remove();
  this.saveInventory();
}

AssetManager.prototype.selectEntry = function(entry) {

  this.domElement.find('.active').removeClass('active');
  entry.children('.entry-display').addClass('active');

  if (entry.data('filetype') == 'dir')
    return;
  $('.url-output').val(this.getAssetURL(entry));
}

AssetManager.prototype.getAssetURL = function(entry) {
  var uuid = entry.data('uuid');
  return 'http://ipfs.strandedin.space:8080/ipfs/' + uuid;
  //return this.baseurl + 'asset/get/' + uuid;
}

AssetManager.prototype.isFolderLoaded = function(folder) {
  return folder.data('loaded') === true;
}

AssetManager.prototype.toggleFolder = function(folder) {
  if (folder.hasClass('expanded')) {
    this.collapseFolder(folder);
  } else {
    this.expandFolder(folder);
  }
}

AssetManager.prototype.expandFolder = function(folder) {
  folder.addClass('expanded');

  if (!this.isFolderLoaded(folder)) {
  //  this.refreshFolder(folder);
  }
}

AssetManager.prototype.collapseFolder = function(folder) {
  folder.removeClass('expanded');
}

AssetManager.prototype.addToFolder = function(folder, node) {
  // No sublist, create one
  if (folder.children('ul').length == 0) {
    folder.append('<ul></ul>');
  }

  var list = folder.children('ul');

  var entryData = node.file;
  var children = node.children;
  var entry = $('<li></li>');

  entry.addClass('entry');
  entry.data('invNode', node);

  //console.log(this.nodeMap.get(this.inventory))
  //console.log(node, entry);
  this.nodeMap.set(node, entry);
  window.nodeMap = this.nodeMap;
  entry.data('filetype', entryData.filetype);
  entry.data('filename', entryData.filename);
  entry.data('uuid', entryData.hash);

  var entryDisplay = $('<div/>');
  entryDisplay.addClass('entry-display');
  entryDisplay.data('entry', entry);

  entry.append(entryDisplay);

  var icon = $('<span/>');
  icon.addClass('icon');

  if (entryData.filetype) {
    icon.addClass('icon-' + entryData.filetype);
  }

  entryDisplay.append('<span class="filename">' + entryData.filename + '</span>');
  entryDisplay.prepend(icon);


  if (entryData.filetype == 'folder') {
    var expander = $('<div/>');
    expander.data('entry', entry);
    expander.addClass('icon expander');
    entry.prepend(expander);
    entry.addClass('folder');
    /*if (Math.random() > 0.6) {
      var target = li;
      this.refreshFolder(target, function() {
        this.expandFolder(target);
      }.bind(this));
    }*/
    //li.addClass('expanded');

    if (children) {
      this.parseInventory(node, entry);
    }
  } else {
    entry.addClass('file');

  }

  list.append(entry);
}

AssetManager.prototype.parseInventory = function(nodes, folder) {

  if (!folder)
    folder = this.domElement;

  for (var i in nodes.children) {
    this.addToFolder(folder, nodes.children[i]);

  }
}

var callbacks = {};
function onJSON(id, cb) {
  callbacks[id] = cb;
}

function getJSON(url, cb) {
  var id = Math.random();
  callbacks[id] = cb;

  if (url.search(/\?/) == -1) {
    url += '?rid=' + id;
  } else {
    url += '&rid=' + id;
  }
  var script = $("<script />", {
    src: url,
    type: "application/javascript"
  });

  $('head').append(script);
}

function consumeJSON(id, json) {
  if (callbacks[id])
    callbacks[id](json);
}

var assetMgr;
gDispatcher.on('ipfsready', function() {
  assetMgr = new AssetManager({
    target: $('.assets-container'),
    host: 'strandedin.space',
    port: 8081
  });
});
