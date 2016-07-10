
// Fires when THREE has loaded.
function onModelViewerReady(view) {
  include('lib/three/loaders/OBJLoader.js').onload = function() {
    initRender(view);
  }
}

include('three_renderer.js');


function initRender(view) {
  var uuid = params.uuid;
  var loader = new THREE.OBJLoader();

  loadViaFrame(assetURL, function(data) {
    var obj = loader.parse(data);

    var mat = new THREE.MeshBasicMaterial({color: 'red'});
    for (var i in obj.children) {
      var child = obj.children[i];
      child.material = mat;
    }
    view.addMesh(obj);

  });

}
