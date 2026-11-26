import argparse
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector


TILE_FOOTPRINT = 1.0
TILE_HEIGHT = 0.12
RIM_DROP = 0.026
RIM_WIDTH = 0.024
TOP_RESOLUTION = 64
SURFACE_NOISE_AMPLITUDE = 0.0022
SURFACE_NOISE_LATTICE = 3
CRACK_DEPTH = 0.011
CRACK_WIDTH = 0.050
CRACK_LENGTH_RANGE = (0.16, 0.74)
CRACK_CENTER_SPREAD = 0.24
CRACK_EDGE_MARGIN = 0.11
CRACK_WANDER_RANGE = (0.0, 0.13)
CRACK_SEGMENT_RANGE = (2, 6)
CRACK_DEPTH_SCALE_RANGE = (0.6, 1.35)
CRACK_WIDTH_SCALE_RANGE = (0.85, 1.3)
CRACK_TAPER_PORTION = 0.3
CRACK_WIDTH_TAPER_FLOOR = 0.7
DEFAULT_CRACK_INTENSITY = 1.0
CHIP_DEPTH = 0.045
CHIP_RADIUS_RANGE = (0.12, 0.19)
PIT_DEPTH_RANGE = (0.004, 0.009)
PIT_RADIUS_RANGE = (0.035, 0.085)
PLANAR_DECIMATE_DEGREES = 1.5

OBSTACLE_HEIGHT = 1.0
OBSTACLE_FOOTPRINT = 0.94
OBSTACLE_BLOCK_RANGE = (1, 2)
OBSTACLE_BLOCK_WIDTH_RANGE = (0.88, 1.0)
OBSTACLE_TAPER_PER_BLOCK = 0.05
OBSTACLE_OFFSET_SPREAD = 0.02
OBSTACLE_TILT_RANGE = (-4.0, 4.0)
OBSTACLE_EDGE_BEVEL = 0.022
OBSTACLE_CHIP_RANGE = (2, 4)
OBSTACLE_CHIP_OFFSET_RANGE = (0.10, 0.22)
OBSTACLE_NOISE_AMPLITUDE = 0.007
OBSTACLE_TONE_LIGHTNESS_SHIFT = -0.085
OBSTACLE_TONE_CHROMA_SCALE = 1.06

ARENA_CELL_SIZE = 1.5
DEFAULT_BASE_COLOR = "C8AE7E"
DEFAULT_CONTRAST = 0.22
LIGHT_CHROMA_SCALE = 0.94
DARK_CHROMA_SCALE = 1.07
TEMPERATURE_SPLIT = 0.020
SUN_COLOR = (1.0, 0.957, 0.839)
SUN_EULER_UNITY = (50.0, -30.0, 0.0)
CAMERA_PITCH = 55.0
CAMERA_YAW = 45.0
GAME_PIXELS_PER_UNIT = 45.0

OBSTACLE_VARIANTS = (
    ("Obstacle_A", 21101),
    ("Obstacle_B", 22203),
    ("Obstacle_C", 23307),
    ("Obstacle_D", 24409),
)

VARIANTS = (
    ("Tile_A", 0, 0, 0, 1101),
    ("Tile_B", 0, 0, 2, 2203),
    ("Tile_C", 1, 0, 1, 3307),
    ("Tile_D", 0, 1, 2, 4409),
    ("Tile_E", 1, 1, 0, 5501),
    ("Tile_F", 1, 0, 3, 6607),
    ("Tile_G", 2, 1, 2, 7703),
    ("Tile_H", 2, 2, 4, 8101),
)


def smoothstep(_t):
    _t = min(1.0, max(0.0, _t))
    return _t * _t * (3.0 - 2.0 * _t)


def srgb_to_linear(_channel):
    if _channel <= 0.04045:
        return _channel / 12.92
    return ((_channel + 0.055) / 1.055) ** 2.4


def linear_to_srgb(_channel):
    if _channel <= 0.0031308:
        return _channel * 12.92
    return 1.055 * (_channel ** (1.0 / 2.4)) - 0.055


def hex_to_linear(_hex):
    _text = _hex.lstrip("#")
    if len(_text) != 6:
        raise ValueError("base colour must be six hex digits, got '%s'" % _hex)
    _channels = [int(_text[_i:_i + 2], 16) / 255.0 for _i in (0, 2, 4)]
    return tuple(srgb_to_linear(_channel) for _channel in _channels)


def linear_to_hex(_linear):
    _channels = [min(1.0, max(0.0, linear_to_srgb(_channel))) for _channel in _linear]
    return "".join("%02X" % int(round(_channel * 255.0)) for _channel in _channels)


def linear_to_oklab(_linear):
    _r, _g, _b = _linear
    _l = 0.4122214708 * _r + 0.5363325363 * _g + 0.0514459929 * _b
    _m = 0.2119034982 * _r + 0.6806995451 * _g + 0.1073969566 * _b
    _s = 0.0883024619 * _r + 0.2817188376 * _g + 0.6299787005 * _b
    _lr = math.copysign(abs(_l) ** (1.0 / 3.0), _l)
    _mr = math.copysign(abs(_m) ** (1.0 / 3.0), _m)
    _sr = math.copysign(abs(_s) ** (1.0 / 3.0), _s)
    return (
        0.2104542553 * _lr + 0.7936177850 * _mr - 0.0040720468 * _sr,
        1.9779984951 * _lr - 2.4285922050 * _mr + 0.4505937099 * _sr,
        0.0259040371 * _lr + 0.7827717662 * _mr - 0.8086757660 * _sr,
    )


def oklab_to_linear(_oklab):
    _lightness, _green_red, _blue_yellow = _oklab
    _lr = _lightness + 0.3963377774 * _green_red + 0.2158037573 * _blue_yellow
    _mr = _lightness - 0.1055613458 * _green_red - 0.0638541728 * _blue_yellow
    _sr = _lightness - 0.0894841775 * _green_red - 1.2914855480 * _blue_yellow
    _l = _lr ** 3
    _m = _mr ** 3
    _s = _sr ** 3
    return (
        max(0.0, 4.0767416621 * _l - 3.3077115913 * _m + 0.2309699292 * _s),
        max(0.0, -1.2684380046 * _l + 2.6097574011 * _m - 0.3413193965 * _s),
        max(0.0, -0.0041960863 * _l - 0.7034186147 * _m + 1.7076147010 * _s),
    )


def derive_tones(_base_linear, _contrast):
    _lightness, _green_red, _blue_yellow = linear_to_oklab(_base_linear)

    _light = (
        _lightness + _contrast * 0.5 * (1.0 - _lightness),
        _green_red * LIGHT_CHROMA_SCALE,
        _blue_yellow * LIGHT_CHROMA_SCALE + _contrast * TEMPERATURE_SPLIT,
    )
    _dark = (
        _lightness - _contrast * 0.5 * _lightness,
        _green_red * DARK_CHROMA_SCALE,
        _blue_yellow * DARK_CHROMA_SCALE - _contrast * TEMPERATURE_SPLIT,
    )
    return oklab_to_linear(_light), oklab_to_linear(_dark)


def derive_obstacle_tone(_linear):
    _lightness, _green_red, _blue_yellow = linear_to_oklab(_linear)
    return oklab_to_linear((
        max(0.0, _lightness + OBSTACLE_TONE_LIGHTNESS_SHIFT),
        _green_red * OBSTACLE_TONE_CHROMA_SCALE,
        _blue_yellow * OBSTACLE_TONE_CHROMA_SCALE,
    ))


def make_value_noise(_seed, _lattice):
    _rng = random.Random(_seed)
    _grid = [[_rng.uniform(-1.0, 1.0) for _ in range(_lattice + 1)] for _ in range(_lattice + 1)]

    def sample(_x, _y):
        _u = (_x + 0.5) * _lattice
        _v = (_y + 0.5) * _lattice
        _i = min(_lattice - 1, max(0, int(math.floor(_u))))
        _j = min(_lattice - 1, max(0, int(math.floor(_v))))
        _fu = smoothstep(_u - _i)
        _fv = smoothstep(_v - _j)
        _a = _grid[_i][_j] * (1.0 - _fu) + _grid[_i + 1][_j] * _fu
        _b = _grid[_i][_j + 1] * (1.0 - _fu) + _grid[_i + 1][_j + 1] * _fu
        return _a * (1.0 - _fv) + _b * _fv

    return sample


def make_crack(_rng):
    _angle = _rng.uniform(0.0, 2.0 * math.pi)
    _length = _rng.uniform(*CRACK_LENGTH_RANGE)
    _segments = _rng.randint(*CRACK_SEGMENT_RANGE)
    _wander = _rng.uniform(*CRACK_WANDER_RANGE)
    _depth_scale = _rng.uniform(*CRACK_DEPTH_SCALE_RANGE)
    _width_scale = _rng.uniform(*CRACK_WIDTH_SCALE_RANGE)

    _direction = Vector((math.cos(_angle), math.sin(_angle)))
    _center = Vector((
        _rng.uniform(-CRACK_CENTER_SPREAD, CRACK_CENTER_SPREAD),
        _rng.uniform(-CRACK_CENTER_SPREAD, CRACK_CENTER_SPREAD),
    ))

    _limit = 0.5 - CRACK_EDGE_MARGIN

    def clamp_inside(_vector):
        return Vector((
            min(_limit, max(-_limit, _vector.x)),
            min(_limit, max(-_limit, _vector.y)),
        ))

    _start = clamp_inside(_center - _direction * _length * 0.5)
    _end = clamp_inside(_center + _direction * _length * 0.5)

    _points = [_start]
    for _step in range(1, _segments):
        _t = _step / float(_segments)
        _straight = _start.lerp(_end, _t)
        _normal = Vector((-(_end.y - _start.y), _end.x - _start.x))
        if _normal.length > 0.0:
            _normal.normalize()
        _offset = _rng.uniform(-_wander, _wander) * math.sin(math.pi * _t)
        _points.append(clamp_inside(_straight + _normal * _offset))
    _points.append(_end)

    return _points, _depth_scale, _width_scale


def closest_point_on_polyline(_point, _polyline):
    _lengths = []
    _total = 0.0
    for _index in range(len(_polyline) - 1):
        _segment_length = (_polyline[_index + 1] - _polyline[_index]).length
        _lengths.append(_segment_length)
        _total += _segment_length

    if _total <= 1e-9:
        return (_point - _polyline[0]).length, 0.5

    _best_distance = float("inf")
    _best_position = 0.0
    _travelled = 0.0

    for _index in range(len(_polyline) - 1):
        _a = _polyline[_index]
        _b = _polyline[_index + 1]
        _segment = _b - _a
        _length_squared = _segment.length_squared
        _t = 0.0 if _length_squared <= 1e-9 else min(1.0, max(0.0, (_point - _a).dot(_segment) / _length_squared))
        _distance = (_point - (_a + _segment * _t)).length
        if _distance < _best_distance:
            _best_distance = _distance
            _best_position = (_travelled + _t * _lengths[_index]) / _total
        _travelled += _lengths[_index]

    return _best_distance, _best_position


def crack_taper(_position):
    _ends = min(_position, 1.0 - _position) / CRACK_TAPER_PORTION
    return smoothstep(_ends)


def make_chips(_rng, _count):
    _anchors = [
        Vector((-0.5, -0.5)), Vector((0.5, -0.5)), Vector((0.5, 0.5)), Vector((-0.5, 0.5)),
        Vector((0.0, -0.5)), Vector((0.5, 0.0)), Vector((0.0, 0.5)), Vector((-0.5, 0.0)),
    ]
    _rng.shuffle(_anchors)
    _chips = []
    for _index in range(_count):
        _center = _anchors[_index % len(_anchors)]
        _radius = _rng.uniform(*CHIP_RADIUS_RANGE)
        _chips.append((_center, _radius))
    return _chips


def make_pits(_rng, _count):
    _pits = []
    for _ in range(_count):
        _center = Vector((_rng.uniform(-0.38, 0.38), _rng.uniform(-0.38, 0.38)))
        _radius = _rng.uniform(*PIT_RADIUS_RANGE)
        _depth = _rng.uniform(*PIT_DEPTH_RANGE)
        _pits.append((_center, _radius, _depth))
    return _pits


def surface_height(_x, _y, _noise, _cracks, _chips, _pits, _crack_intensity):
    _rim_distance = min(0.5 - abs(_x), 0.5 - abs(_y))
    _height = -RIM_DROP * (1.0 - smoothstep(_rim_distance / RIM_WIDTH))
    _height += _noise(_x, _y) * SURFACE_NOISE_AMPLITUDE

    _point = Vector((_x, _y))
    for _points, _depth_scale, _width_scale in _cracks:
        _distance, _position = closest_point_on_polyline(_point, _points)
        _taper = crack_taper(_position)
        if _taper <= 0.0:
            continue
        _width = CRACK_WIDTH * _width_scale * (CRACK_WIDTH_TAPER_FLOOR + (1.0 - CRACK_WIDTH_TAPER_FLOOR) * _taper)
        if _distance < _width:
            _falloff = 1.0 - _distance / _width
            _height -= CRACK_DEPTH * _depth_scale * _crack_intensity * _taper * _falloff * _falloff

    for _center, _radius in _chips:
        _distance = (_point - _center).length
        if _distance < _radius:
            _falloff = 1.0 - _distance / _radius
            _height -= CHIP_DEPTH * smoothstep(_falloff)

    for _center, _radius, _depth in _pits:
        _distance = (_point - _center).length
        if _distance < _radius:
            _falloff = 1.0 - _distance / _radius
            _height -= _depth * smoothstep(_falloff)

    return _height


def build_tile_object(_name, _crack_count, _chip_count, _pit_count, _seed, _crack_intensity):
    _rng = random.Random(_seed)
    _noise = make_value_noise(_seed + 1, SURFACE_NOISE_LATTICE)
    _cracks = [make_crack(_rng) for _ in range(_crack_count)]
    _chips = make_chips(_rng, _chip_count)
    _pits = make_pits(_rng, _pit_count)

    _bm = bmesh.new()
    _half = TILE_FOOTPRINT * 0.5
    _step = TILE_FOOTPRINT / TOP_RESOLUTION

    _top = []
    for _i in range(TOP_RESOLUTION + 1):
        _column = []
        for _j in range(TOP_RESOLUTION + 1):
            _x = -_half + _i * _step
            _y = -_half + _j * _step
            _z = surface_height(_x, _y, _noise, _cracks, _chips, _pits, _crack_intensity)
            _column.append(_bm.verts.new((_x, _y, _z)))
        _top.append(_column)

    for _i in range(TOP_RESOLUTION):
        for _j in range(TOP_RESOLUTION):
            _bm.faces.new((_top[_i][_j], _top[_i + 1][_j], _top[_i + 1][_j + 1], _top[_i][_j + 1]))

    _ring = []
    for _i in range(TOP_RESOLUTION):
        _ring.append(_top[_i][0])
    for _j in range(TOP_RESOLUTION):
        _ring.append(_top[TOP_RESOLUTION][_j])
    for _i in range(TOP_RESOLUTION, 0, -1):
        _ring.append(_top[_i][TOP_RESOLUTION])
    for _j in range(TOP_RESOLUTION, 0, -1):
        _ring.append(_top[0][_j])

    _bottom = [_bm.verts.new((_vert.co.x, _vert.co.y, -TILE_HEIGHT)) for _vert in _ring]

    for _index in range(len(_ring)):
        _next = (_index + 1) % len(_ring)
        _bm.faces.new((_ring[_index], _bottom[_index], _bottom[_next], _ring[_next]))

    _bm.faces.new(list(reversed(_bottom)))

    bmesh.ops.recalc_face_normals(_bm, faces=_bm.faces)

    _uv_layer = _bm.loops.layers.uv.new("UVMap")
    for _face in _bm.faces:
        _face.smooth = False
        for _loop in _face.loops:
            _loop[_uv_layer].uv = (_loop.vert.co.x + _half, _loop.vert.co.y + _half)

    _mesh = bpy.data.meshes.new(_name)
    _bm.to_mesh(_mesh)
    _bm.free()

    _object = bpy.data.objects.new(_name, _mesh)
    bpy.context.scene.collection.objects.link(_object)
    return _object


def build_stone_block(_rng, _width, _depth, _height, _center, _tilt_degrees):
    _bm = bmesh.new()
    bmesh.ops.create_cube(_bm, size=1.0)
    bmesh.ops.scale(_bm, vec=Vector((_width, _depth, _height)), verts=_bm.verts)

    _corners = list(_bm.verts)
    _rng.shuffle(_corners)
    for _index in range(_rng.randint(*OBSTACLE_CHIP_RANGE)):
        if _index >= len(_corners):
            break
        bmesh.ops.bevel(
            _bm,
            geom=[_corners[_index]],
            offset=_rng.uniform(*OBSTACLE_CHIP_OFFSET_RANGE),
            segments=1,
            affect="VERTICES",
            clamp_overlap=True,
        )

    bmesh.ops.bevel(
        _bm,
        geom=list(_bm.verts) + list(_bm.edges) + list(_bm.faces),
        offset=OBSTACLE_EDGE_BEVEL,
        segments=1,
        affect="EDGES",
        clamp_overlap=True,
    )

    for _vert in _bm.verts:
        _vert.co += Vector((
            _rng.uniform(-1.0, 1.0),
            _rng.uniform(-1.0, 1.0),
            _rng.uniform(-1.0, 1.0),
        )) * OBSTACLE_NOISE_AMPLITUDE

    bmesh.ops.rotate(
        _bm,
        verts=_bm.verts,
        cent=Vector((0.0, 0.0, 0.0)),
        matrix=Matrix.Rotation(math.radians(_tilt_degrees), 3, "Z"),
    )
    bmesh.ops.translate(_bm, verts=_bm.verts, vec=_center)
    return _bm


def build_obstacle_object(_name, _seed):
    _rng = random.Random(_seed)
    _block_count = _rng.randint(*OBSTACLE_BLOCK_RANGE)

    _weights = [_rng.uniform(0.6, 1.0) for _ in range(_block_count)]
    _total = sum(_weights)
    _heights = [OBSTACLE_HEIGHT * _weight / _total for _weight in _weights]

    _bm = bmesh.new()
    _z = 0.0
    for _index in range(_block_count):
        _taper = 1.0 - OBSTACLE_TAPER_PER_BLOCK * _index
        _height = _heights[_index]
        _width = OBSTACLE_FOOTPRINT * _rng.uniform(*OBSTACLE_BLOCK_WIDTH_RANGE) * _taper
        _depth = OBSTACLE_FOOTPRINT * _rng.uniform(*OBSTACLE_BLOCK_WIDTH_RANGE) * _taper
        _center = Vector((
            _rng.uniform(-OBSTACLE_OFFSET_SPREAD, OBSTACLE_OFFSET_SPREAD) * _index,
            _rng.uniform(-OBSTACLE_OFFSET_SPREAD, OBSTACLE_OFFSET_SPREAD) * _index,
            _z + _height * 0.5,
        ))
        _block = build_stone_block(_rng, _width, _depth, _height, _center, _rng.uniform(*OBSTACLE_TILT_RANGE))

        _temporary = bpy.data.meshes.new("BlockTemp")
        _block.to_mesh(_temporary)
        _block.free()
        _bm.from_mesh(_temporary)
        bpy.data.meshes.remove(_temporary)

        _z += _height

    _uv_layer = _bm.loops.layers.uv.new("UVMap")
    for _face in _bm.faces:
        _face.smooth = False
        for _loop in _face.loops:
            _loop[_uv_layer].uv = (_loop.vert.co.x + 0.5, _loop.vert.co.y + 0.5)

    _mesh = bpy.data.meshes.new(_name)
    _bm.to_mesh(_mesh)
    _bm.free()

    _object = bpy.data.objects.new(_name, _mesh)
    bpy.context.scene.collection.objects.link(_object)
    return _object


def decimate_planar(_object, _degrees):
    _modifier = _object.modifiers.new("PlanarDecimate", "DECIMATE")
    _modifier.decimate_type = "DISSOLVE"
    _modifier.angle_limit = math.radians(_degrees)
    _modifier.delimit = {"NORMAL"}

    _triangulate = _object.modifiers.new("Triangulate", "TRIANGULATE")
    _triangulate.keep_custom_normals = False

    bpy.context.view_layer.objects.active = _object
    bpy.ops.object.modifier_apply(modifier=_modifier.name)
    bpy.ops.object.modifier_apply(modifier=_triangulate.name)


def make_stone_material(_name, _color):
    _material = bpy.data.materials.new(_name)
    _material.use_nodes = True
    _bsdf = _material.node_tree.nodes["Principled BSDF"]
    _bsdf.inputs["Base Color"].default_value = (_color[0], _color[1], _color[2], 1.0)
    _bsdf.inputs["Roughness"].default_value = 0.88
    if "Specular IOR Level" in _bsdf.inputs:
        _bsdf.inputs["Specular IOR Level"].default_value = 0.2
    return _material


def unity_direction_to_blender(_euler_degrees):
    _pitch = math.radians(_euler_degrees[0])
    _yaw = math.radians(_euler_degrees[1])
    _unity = Vector((
        math.cos(_pitch) * math.sin(_yaw),
        -math.sin(_pitch),
        math.cos(_pitch) * math.cos(_yaw),
    ))
    return Vector((_unity.x, _unity.z, _unity.y))


def coordinate_hash(_x, _y, _salt):
    _hash = (_x * 73856093) ^ (_y * 19349663) ^ (_salt * 83492791)
    _hash = (_hash ^ (_hash >> 13)) * 1274126177
    return abs(_hash ^ (_hash >> 16))


def build_preview_scene(_tiles, _obstacles, _span, _light_tone, _dark_tone):
    _light_material = make_stone_material("PreviewLightStone", _light_tone)
    _dark_material = make_stone_material("PreviewDarkStone", _dark_tone)

    _half = _span // 2
    for _x in range(-_half, _half + 1):
        for _y in range(-_half, _half + 1):
            _source = _tiles[coordinate_hash(_x, _y, 1) % len(_tiles)]
            _instance = _source.copy()
            _instance.location = (_x * ARENA_CELL_SIZE, _y * ARENA_CELL_SIZE, 0.0)
            _instance.scale = (ARENA_CELL_SIZE, ARENA_CELL_SIZE, ARENA_CELL_SIZE)
            _instance.rotation_euler = (0.0, 0.0, math.radians(90.0 * (coordinate_hash(_x, _y, 2) % 4)))
            _instance.data = _source.data.copy()
            _instance.data.materials.clear()
            _instance.data.materials.append(_light_material if (_x + _y) % 2 == 0 else _dark_material)
            bpy.context.scene.collection.objects.link(_instance)

    _obstacle_material = make_stone_material("PreviewObstacleStone", derive_obstacle_tone(_dark_tone))
    _obstacle_cells = ((-2, 1), (0, 0), (2, -2), (1, 3), (-3, -2), (3, 2))
    for _index, (_x, _y) in enumerate(_obstacle_cells):
        _source = _obstacles[_index % len(_obstacles)]
        _instance = _source.copy()
        _instance.location = (_x * ARENA_CELL_SIZE, _y * ARENA_CELL_SIZE, 0.0)
        _instance.scale = (ARENA_CELL_SIZE, ARENA_CELL_SIZE, ARENA_CELL_SIZE)
        _instance.rotation_euler = (0.0, 0.0, math.radians(90.0 * (coordinate_hash(_x, _y, 3) % 4)))
        _instance.data = _source.data.copy()
        _instance.data.materials.clear()
        _instance.data.materials.append(_obstacle_material)
        bpy.context.scene.collection.objects.link(_instance)

    for _tile in _tiles:
        _tile.hide_render = True

    for _obstacle in _obstacles:
        _obstacle.hide_render = True

    _sun_data = bpy.data.lights.new("Sun", "SUN")
    _sun_data.energy = 3.1
    _sun_data.color = SUN_COLOR
    _sun_data.angle = math.radians(3.0)
    _sun = bpy.data.objects.new("Sun", _sun_data)
    bpy.context.scene.collection.objects.link(_sun)
    _sun.rotation_mode = "QUATERNION"
    _sun.rotation_quaternion = unity_direction_to_blender(SUN_EULER_UNITY).to_track_quat("-Z", "Y")

    _world = bpy.data.worlds.new("PreviewWorld")
    _world.use_nodes = True
    _world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.42, 0.46, 0.55, 1.0)
    _world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.55
    bpy.context.scene.world = _world

    _camera_data = bpy.data.cameras.new("Camera")
    _camera_data.type = "ORTHO"
    _camera_data.clip_start = 1.0
    _camera_data.clip_end = 200.0
    _camera = bpy.data.objects.new("Camera", _camera_data)
    bpy.context.scene.collection.objects.link(_camera)

    _pitch = math.radians(CAMERA_PITCH)
    _yaw = math.radians(CAMERA_YAW)
    _unity_forward = Vector((
        math.cos(_pitch) * math.sin(_yaw),
        -math.sin(_pitch),
        math.cos(_pitch) * math.cos(_yaw),
    ))
    _forward = Vector((_unity_forward.x, _unity_forward.z, _unity_forward.y))
    _camera.location = -_forward * 50.0
    _camera.rotation_mode = "QUATERNION"
    _camera.rotation_quaternion = _forward.to_track_quat("-Z", "Y")
    bpy.context.scene.camera = _camera
    return _camera


def render_preview(_camera, _path, _resolution, _zoom):
    _scene = bpy.context.scene
    _scene.render.engine = "CYCLES"
    _scene.cycles.samples = 48
    _scene.cycles.use_denoising = True
    _scene.render.resolution_x = _resolution
    _scene.render.resolution_y = _resolution
    _scene.render.film_transparent = False
    _scene.view_settings.view_transform = "Standard"
    _camera.data.ortho_scale = _resolution / (GAME_PIXELS_PER_UNIT * _zoom)
    _scene.render.filepath = _path
    bpy.ops.render.render(write_still=True)


def export_fbx(_path, _names):
    for _object in bpy.context.scene.objects:
        _object.hide_render = False
    bpy.ops.object.select_all(action="DESELECT")
    for _name in _names:
        bpy.data.objects[_name].select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=_path,
        use_selection=True,
        apply_unit_scale=True,
        global_scale=1.0,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        path_mode="STRIP",
    )


def parse_arguments():
    _argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    _parser = argparse.ArgumentParser()
    _parser.add_argument("--out", required=True)
    _parser.add_argument("--fbx", default=None)
    _parser.add_argument("--obstacle-fbx", default=None)
    _parser.add_argument("--preview-span", type=int, default=7)
    _parser.add_argument("--no-decimate", action="store_true")
    _parser.add_argument("--base-color", default=DEFAULT_BASE_COLOR)
    _parser.add_argument("--contrast", type=float, default=DEFAULT_CONTRAST)
    _parser.add_argument("--crack-intensity", type=float, default=DEFAULT_CRACK_INTENSITY)
    _parser.add_argument("--label", default=None)
    _parser.add_argument("--only-mid", action="store_true")
    return _parser.parse_args(_argv)


def main():
    _arguments = parse_arguments()
    os.makedirs(_arguments.out, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.scale_length = 1.0

    _tiles = []
    for _name, _crack_count, _chip_count, _pit_count, _seed in VARIANTS:
        _tile = build_tile_object(_name, _crack_count, _chip_count, _pit_count, _seed, _arguments.crack_intensity)
        if not _arguments.no_decimate:
            decimate_planar(_tile, PLANAR_DECIMATE_DEGREES)
        _tiles.append(_tile)
        print("TILE %s tris=%d verts=%d" % (_name, len(_tile.data.polygons), len(_tile.data.vertices)))

    _obstacles = []
    for _name, _seed in OBSTACLE_VARIANTS:
        _obstacle = build_obstacle_object(_name, _seed)
        _obstacles.append(_obstacle)
        print("OBSTACLE %s tris=%d verts=%d" % (_name, len(_obstacle.data.polygons), len(_obstacle.data.vertices)))

    _base_linear = hex_to_linear(_arguments.base_color)
    _light_tone, _dark_tone = derive_tones(_base_linear, _arguments.contrast)
    print("PALETTE base=%s contrast=%.2f light=%s dark=%s" % (
        linear_to_hex(_base_linear),
        _arguments.contrast,
        linear_to_hex(_light_tone),
        linear_to_hex(_dark_tone),
    ))

    _camera = build_preview_scene(_tiles, _obstacles, _arguments.preview_span, _light_tone, _dark_tone)
    _suffix = "_%s" % _arguments.label if _arguments.label else ""
    render_preview(_camera, os.path.join(_arguments.out, "tiles_mid%s.png" % _suffix), 1080, 1.7)
    if not _arguments.only_mid:
        render_preview(_camera, os.path.join(_arguments.out, "tiles_gamesize%s.png" % _suffix), 1080, 1.0)
        render_preview(_camera, os.path.join(_arguments.out, "tiles_zoomed%s.png" % _suffix), 1080, 2.8)

    if _arguments.fbx:
        os.makedirs(os.path.dirname(_arguments.fbx), exist_ok=True)
        export_fbx(_arguments.fbx, [_variant[0] for _variant in VARIANTS])
        print("FBX %s" % _arguments.fbx)

    if _arguments.obstacle_fbx:
        os.makedirs(os.path.dirname(_arguments.obstacle_fbx), exist_ok=True)
        export_fbx(_arguments.obstacle_fbx, [_variant[0] for _variant in OBSTACLE_VARIANTS])
        print("OBSTACLE_FBX %s" % _arguments.obstacle_fbx)

    if not _arguments.only_mid:
        _blend_path = os.path.join(_arguments.out, "arena_tiles.blend")
        bpy.ops.wm.save_as_mainfile(filepath=_blend_path)
        print("BLEND %s" % _blend_path)


main()
