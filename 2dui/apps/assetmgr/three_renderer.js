var modelView;

// Lol...At least these are local files.
include('lib/three/three.min.js').onload = function() {
  include('lib/three/CanvasRenderer.js').onload = function() {
    include('lib/three/Projector.js').onload = function() {
      modelView = new ModelView();
    }
  }
}

function ModelView() {
  this.renderer = new THREE.CanvasRenderer();
  var domEl = this.renderer.domElement;
  $('.windowarea').append(domEl);

  this.renderer.setSize(window.innerWidth, window.innerHeight);
  domEl.style.width = '100%';
  domEl.style.height = '100%';

  this.scene = new THREE.Scene();
  this.camera = new THREE.PerspectiveCamera(75, window.innerWidth/window.innerHeight, 0.1, 100);

  var cubeGeo = new THREE.BoxGeometry(1,1,1);
  var cubeMat = new THREE.MeshBasicMaterial({color: 'red'});

  var cube = new THREE.Mesh(cubeGeo, cubeMat);

  //this.scene.add(cube);
  this.camera.position.set(0,0,10);

  if (typeof onModelViewerReady == 'function')
    onModelViewerReady(this);

  window.addEventListener('resize', function() {
    this.renderer.setSize(window.innerWidth, window.innerHeight);

    this.camera.aspect = window.innerWidth/window.innerHeight;
    this.camera.updateProjectionMatrix();
    this.render();
  }.bind(this));
  this.frame = this.frame.bind(this);
  this.frame();
}

ModelView.prototype.addMesh = function(mesh) {
  this.scene.add(mesh);
  //this.render();
}

ModelView.prototype.frame = function() {
  var t = performance.now() * 0.001;
  var d = 10;
  this.camera.position.y = 1;
  this.camera.position.x = Math.sin(t);
  this.camera.position.z = Math.cos(t);

  this.camera.lookAt(this.scene.position);
  //requestAnimationFrame(this.frame);
  this.render();

  setTimeout(function() {
    this.frame();
  }.bind(this), 40);
}

ModelView.prototype.render = function() {
  this.renderer.render(this.scene, this.camera);
}
