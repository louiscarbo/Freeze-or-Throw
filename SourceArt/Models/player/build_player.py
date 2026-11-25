import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = Path(__file__).resolve().parent
TEXTURE_DIRECTORY = OUTPUT_DIRECTORY.parent / "textures"

WOOD_TILES_AROUND = 2
WOOD_TILE_METRES = 0.8
KNIT_TILE_METRES = 0.19
KNIT_TILES_AROUND_HAT = 7
KNIT_TILES_AROUND_SCARF = 7
KNIT_TILES_ACROSS_SCARF = 2
KNIT_TILES_AROUND_POMPOM = 2
KNIT_NORMAL_STRENGTH = 1.0

WOOD_HEX = "#D9B382"
FACE_DARK_HEX = "#4A3A33"
CARROT_HEX = "#F08A2C"
PLAYER_COLOUR_HEXES = {
    "Blue": "#3E5BA9",
    "Red": "#C9463D",
    "Green": "#4E9A5B",
    "Yellow": "#E8B63A",
}
STAGE_HEX = "#F8F1E6"

ROUND_SEGMENT_COUNT = 14

BODY_PROFILE = [
    (0.0, 0.0),
    (0.20, 0.0),
    (0.268, 0.035),
    (0.29, 0.11),
    (0.275, 0.21),
    (0.235, 0.32),
    (0.185, 0.42),
    (0.14, 0.50),
    (0.12, 0.54),
]

HEAD_CENTRE_HEIGHT = 0.67
HEAD_RADIUS = 0.20
HEAD_LATITUDE_COUNT = 8

EYE_RADIUS = 0.021
EYE_SEGMENT_COUNT = 10
EYE_EMBED_DEPTH = 0.008
EYE_PROUD_HEIGHT = 0.009
EYE_RIM_BEVEL = 0.0045
EYE_AZIMUTH_DEGREES = 24
EYE_HEIGHT = 0.652
NOSE_HEIGHT = 0.612
NOSE_LENGTH = 0.06
SMILE_HALF_WIDTH_DEGREES = 10
SMILE_CENTRE_HEIGHT = 0.566
SMILE_CORNER_LIFT = 0.012
SMILE_HALF_THICKNESS = 0.0045
SMILE_RAISE_FACTOR = 1.02
SMILE_SAMPLE_COUNT = 7

HAT_BRIM_SEGMENT_COUNT = 28
HAT_BRIM_RIB_DEPTH = 0.007
HAT_BRIM_PROFILE = [
    (0.201, 0.700),
    (0.214, 0.696),
    (0.221, 0.716),
    (0.221, 0.750),
    (0.214, 0.772),
    (0.203, 0.769),
]
HAT_BRIM_RIBBED_POINT_INDICES = {1, 2, 3, 4}
HAT_DOME_PROFILE = [
    (0.213, 0.745),
    (0.209, 0.776),
    (0.192, 0.822),
    (0.158, 0.870),
    (0.108, 0.905),
    (0.052, 0.925),
    (0.0, 0.931),
]
POMPOM_CENTRE_HEIGHT = 0.946
POMPOM_RADIUS = 0.056
HAT_TILT_BACK_DEGREES = 11

SCARF_CENTRE_HEIGHT = 0.50
SCARF_MAJOR_RADIUS = 0.165
SCARF_RADIAL_HALF_THICKNESS = 0.05
SCARF_HALF_HEIGHT = 0.052
SCARF_PROFILE_POINT_COUNT = 6
SCARF_TAIL_AZIMUTH_DEGREES = 32
SCARF_TILT_DEGREES = 5
SCARF_TAIL_THICKNESS = 0.044
SCARF_TAIL_CHAMFER = 0.02
SCARF_TAIL_RINGS = [
    {"height": 0.528, "width": 0.105, "sideways_shift": 0.0, "thickness_factor": 1.0},
    {"height": 0.44, "width": 0.112, "sideways_shift": 0.004, "thickness_factor": 1.0},
    {"height": 0.34, "width": 0.118, "sideways_shift": 0.012, "thickness_factor": 1.0},
    {"height": 0.255, "width": 0.124, "sideways_shift": 0.022, "thickness_factor": 1.0},
    {"height": 0.228, "width": 0.092, "sideways_shift": 0.025, "thickness_factor": 0.6},
]


def srgb_channel_to_linear(channel):
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def hex_to_linear_rgba(hex_colour):
    stripped = hex_colour.lstrip("#")
    channels = [int(stripped[index:index + 2], 16) / 255 for index in (0, 2, 4)]
    return [srgb_channel_to_linear(channel) for channel in channels] + [1.0]


def create_material(name, hex_colour, roughness=0.8):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    node_tree = material.node_tree
    principled = next((node for node in node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
    if principled is None:
        principled = node_tree.nodes.new("ShaderNodeBsdfPrincipled")
        output = node_tree.nodes.new("ShaderNodeOutputMaterial")
        node_tree.links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    colour = hex_to_linear_rgba(hex_colour)
    principled.inputs["Base Color"].default_value = colour
    principled.inputs["Roughness"].default_value = roughness
    material.diffuse_color = colour
    return material


def load_texture(file_name, is_colour):
    image = bpy.data.images.load(str(TEXTURE_DIRECTORY / file_name), check_existing=True)
    if not is_colour:
        image.colorspace_settings.name = "Non-Color"
    return image


def add_image_node(material, file_name, is_colour):
    image_node = material.node_tree.nodes.new("ShaderNodeTexImage")
    image_node.image = load_texture(file_name, is_colour)
    return image_node


def principled_node(material):
    return next(node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED")


def create_wood_material(name):
    material = create_material(name, WOOD_HEX)
    albedo = add_image_node(material, "wood-albedo.png", is_colour=True)
    material.node_tree.links.new(albedo.outputs["Color"], principled_node(material).inputs["Base Color"])
    return material


def create_knit_material(name, hex_colour):
    material = create_material(name, hex_colour)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = principled_node(material)

    colour_node = nodes.new("ShaderNodeRGB")
    colour_node.name = "PlayerColour"
    colour_node.label = "PlayerColour"
    colour_node.outputs[0].default_value = hex_to_linear_rgba(hex_colour)

    albedo = add_image_node(material, "knit-albedo.png", is_colour=True)
    multiply = nodes.new("ShaderNodeMix")
    multiply.data_type = "RGBA"
    multiply.blend_type = "MULTIPLY"
    next(socket for socket in multiply.inputs if socket.name == "Factor" and socket.type == "VALUE").default_value = 1.0
    colour_inputs = [socket for socket in multiply.inputs if socket.type == "RGBA"]
    links.new(albedo.outputs["Color"], colour_inputs[0])
    links.new(colour_node.outputs[0], colour_inputs[1])
    links.new(next(socket for socket in multiply.outputs if socket.type == "RGBA"), principled.inputs["Base Color"])

    normal_image = add_image_node(material, "knit-normal.png", is_colour=False)
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.inputs["Strength"].default_value = KNIT_NORMAL_STRENGTH
    links.new(normal_image.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])
    return material


def profile_coordinates(profile, metres_per_tile, closed_profile=False):
    points = list(profile) + ([profile[0]] if closed_profile else [])
    coordinates = [0.0]
    for (lower_radius, lower_height), (upper_radius, upper_height) in zip(points, points[1:]):
        step = math.hypot(upper_radius - lower_radius, upper_height - lower_height)
        coordinates.append(coordinates[-1] + step / metres_per_tile)
    return coordinates


def set_face_uvs(mesh_builder, face, uvs):
    uv_layer = mesh_builder.loops.layers.uv.verify()
    for loop, uv in zip(face.loops, uvs):
        loop[uv_layer].uv = uv


def add_lathe(
    mesh_builder,
    profile,
    segment_count,
    material_index,
    closed_profile=False,
    radius_offset=None,
    tiles_around=1,
    along_coordinates=None,
    around_is_texture_vertical=False,
):
    rings = []
    for point_index, (radius, height) in enumerate(profile):
        if radius == 0.0:
            rings.append(mesh_builder.verts.new((0.0, 0.0, height)))
            continue
        ring = []
        for segment_index in range(segment_count):
            angle = 2 * math.pi * segment_index / segment_count
            offset = radius_offset(point_index, segment_index) if radius_offset else 0.0
            ring.append(mesh_builder.verts.new((
                (radius + offset) * math.cos(angle),
                (radius + offset) * math.sin(angle),
                height,
            )))
        rings.append(ring)

    if along_coordinates is None:
        along_coordinates = [0.0] * (len(profile) + 1)

    def texture_coordinate(segment_position, along):
        around = segment_position / segment_count * tiles_around
        return (along, around) if around_is_texture_vertical else (around, along)

    ring_pairs = list(zip(rings, rings[1:]))
    if closed_profile:
        ring_pairs.append((rings[-1], rings[0]))

    for pair_index, (lower, upper) in enumerate(ring_pairs):
        lower_along = along_coordinates[pair_index]
        upper_along = along_coordinates[pair_index + 1]
        for segment_index in range(segment_count):
            next_index = (segment_index + 1) % segment_count
            if not isinstance(lower, list):
                corners = [
                    (lower, segment_index + 0.5, lower_along),
                    (upper[next_index], segment_index + 1, upper_along),
                    (upper[segment_index], segment_index, upper_along),
                ]
            elif not isinstance(upper, list):
                corners = [
                    (upper, segment_index + 0.5, upper_along),
                    (lower[segment_index], segment_index, lower_along),
                    (lower[next_index], segment_index + 1, lower_along),
                ]
            else:
                corners = [
                    (lower[segment_index], segment_index, lower_along),
                    (lower[next_index], segment_index + 1, lower_along),
                    (upper[next_index], segment_index + 1, upper_along),
                    (upper[segment_index], segment_index, upper_along),
                ]
            face = mesh_builder.faces.new([corner[0] for corner in corners])
            face.material_index = material_index
            set_face_uvs(mesh_builder, face, [texture_coordinate(corner[1], corner[2]) for corner in corners])


def add_primitive(mesh_builder, material_index, create):
    faces_before = set(mesh_builder.faces)
    create()
    new_faces = [face for face in mesh_builder.faces if face not in faces_before]
    for face in new_faces:
        face.material_index = material_index
    return new_faces


def sphere_profile(centre_height, radius, latitude_count):
    profile = []
    for latitude_index in range(latitude_count + 1):
        angle = -math.pi / 2 + math.pi * latitude_index / latitude_count
        ring_radius = 0.0 if latitude_index in (0, latitude_count) else radius * math.cos(angle)
        profile.append((ring_radius, centre_height + radius * math.sin(angle)))
    return profile


def point_on_head(azimuth_degrees, height, depth_factor=1.0):
    azimuth = math.radians(azimuth_degrees)
    vertical = height - HEAD_CENTRE_HEIGHT
    horizontal = math.sqrt(HEAD_RADIUS ** 2 - vertical ** 2) * depth_factor
    return Vector((horizontal * math.sin(azimuth), -horizontal * math.cos(azimuth), height))


def body_radius_at(height):
    for (lower_radius, lower_height), (upper_radius, upper_height) in zip(BODY_PROFILE[1:], BODY_PROFILE[2:]):
        if lower_height <= height <= upper_height:
            blend = (height - lower_height) / (upper_height - lower_height)
            return lower_radius + (upper_radius - lower_radius) * blend
    return BODY_PROFILE[-1][0]


def finish_object(name, mesh_builder, materials, collection):
    mesh = bpy.data.meshes.new(name)
    mesh_builder.to_mesh(mesh)
    mesh_builder.free()
    for material in materials:
        mesh.materials.append(material)
    for polygon in mesh.polygons:
        polygon.use_smooth = False

    corners = [Vector(vertex.co) for vertex in mesh.vertices]
    minimum = Vector([min(corner[axis] for corner in corners) for axis in range(3)])
    maximum = Vector([max(corner[axis] for corner in corners) for axis in range(3)])
    centre = (minimum + maximum) / 2
    mesh.transform(Matrix.Translation(-centre))
    mesh.update()

    mesh_object = bpy.data.objects.new(name, mesh)
    mesh_object.location = centre
    collection.objects.link(mesh_object)
    return mesh_object


def add_smile(mesh_builder, material_index):
    head_centre = Vector((0.0, 0.0, HEAD_CENTRE_HEIGHT))
    samples = []
    for sample_index in range(SMILE_SAMPLE_COUNT):
        across = sample_index / (SMILE_SAMPLE_COUNT - 1) * 2 - 1
        azimuth_degrees = across * SMILE_HALF_WIDTH_DEGREES
        height = SMILE_CENTRE_HEIGHT + SMILE_CORNER_LIFT * across ** 2
        samples.append((
            mesh_builder.verts.new(point_on_head(azimuth_degrees, height + SMILE_HALF_THICKNESS, 0.995)),
            mesh_builder.verts.new(point_on_head(azimuth_degrees, height, SMILE_RAISE_FACTOR)),
            mesh_builder.verts.new(point_on_head(azimuth_degrees, height - SMILE_HALF_THICKNESS, 0.995)),
        ))
    for (upper, raised, lower), (next_upper, next_raised, next_lower) in zip(samples, samples[1:]):
        for corners in ((upper, next_upper, next_raised, raised), (raised, next_raised, next_lower, lower)):
            face = mesh_builder.faces.new(corners)
            face.material_index = material_index
            face.normal_update()
            if face.normal.dot(face.calc_center_median() - head_centre) < 0:
                face.normal_flip()

    smile_centre = point_on_head(0, SMILE_CENTRE_HEIGHT)
    for end_sample in (samples[0], samples[-1]):
        end_cap = mesh_builder.faces.new(end_sample)
        end_cap.material_index = material_index
        end_cap.normal_update()
        if end_cap.normal.dot(end_cap.calc_center_median() - smile_centre) < 0:
            end_cap.normal_flip()


def add_button_eye(mesh_builder, centre, material_index):
    outward = (centre - Vector((0.0, 0.0, HEAD_CENTRE_HEIGHT))).normalized()
    across = outward.cross(Vector((0.0, 0.0, 1.0))).normalized()
    upward = across.cross(outward).normalized()

    def ring(radius, depth):
        vertices = []
        for segment_index in range(EYE_SEGMENT_COUNT):
            angle = 2 * math.pi * segment_index / EYE_SEGMENT_COUNT
            position = centre + outward * depth + (across * math.cos(angle) + upward * math.sin(angle)) * radius
            vertices.append(mesh_builder.verts.new(position))
        return vertices

    base_ring = ring(EYE_RADIUS, -EYE_EMBED_DEPTH)
    rim_ring = ring(EYE_RADIUS, EYE_PROUD_HEIGHT - EYE_RIM_BEVEL)
    front_ring = ring(EYE_RADIUS - EYE_RIM_BEVEL, EYE_PROUD_HEIGHT)

    faces = [mesh_builder.faces.new(front_ring)]
    for lower, upper in ((base_ring, rim_ring), (rim_ring, front_ring)):
        for segment_index in range(EYE_SEGMENT_COUNT):
            next_index = (segment_index + 1) % EYE_SEGMENT_COUNT
            faces.append(mesh_builder.faces.new((
                lower[segment_index], lower[next_index], upper[next_index], upper[segment_index])))
    for face in faces:
        face.material_index = material_index
        face.normal_update()
        if face.normal.dot(face.calc_center_median() - centre) < 0:
            face.normal_flip()


def build_body(collection, wood, face_dark, carrot):
    mesh_builder = bmesh.new()
    add_lathe(
        mesh_builder,
        BODY_PROFILE,
        ROUND_SEGMENT_COUNT,
        0,
        tiles_around=WOOD_TILES_AROUND,
        along_coordinates=profile_coordinates(BODY_PROFILE, WOOD_TILE_METRES),
    )
    head_profile = sphere_profile(HEAD_CENTRE_HEIGHT, HEAD_RADIUS, HEAD_LATITUDE_COUNT)
    add_lathe(
        mesh_builder,
        head_profile,
        ROUND_SEGMENT_COUNT,
        0,
        tiles_around=WOOD_TILES_AROUND,
        along_coordinates=profile_coordinates(head_profile, WOOD_TILE_METRES),
    )

    for side in (-1, 1):
        add_button_eye(mesh_builder, point_on_head(side * EYE_AZIMUTH_DEGREES, EYE_HEIGHT), 1)

    nose_matrix = (
        Matrix.Translation(point_on_head(0, NOSE_HEIGHT) + Vector((0.0, -NOSE_LENGTH * 0.3, 0.0)))
        @ Matrix.Rotation(math.radians(90), 4, "X")
    )
    add_primitive(mesh_builder, 2, lambda: bmesh.ops.create_cone(
        mesh_builder,
        cap_ends=True,
        segments=6,
        radius1=0.03,
        radius2=0.012,
        depth=NOSE_LENGTH,
        matrix=nose_matrix,
    ))
    add_smile(mesh_builder, 1)
    return finish_object("Body", mesh_builder, [wood, face_dark, carrot], collection)


def build_hat(collection, player_colour):
    mesh_builder = bmesh.new()

    def brim_rib_offset(point_index, segment_index):
        if point_index in HAT_BRIM_RIBBED_POINT_INDICES and segment_index % 2 == 1:
            return -HAT_BRIM_RIB_DEPTH
        return 0.0

    add_lathe(
        mesh_builder,
        HAT_BRIM_PROFILE,
        HAT_BRIM_SEGMENT_COUNT,
        0,
        closed_profile=True,
        radius_offset=brim_rib_offset,
        tiles_around=KNIT_TILES_AROUND_HAT,
        along_coordinates=profile_coordinates(HAT_BRIM_PROFILE, KNIT_TILE_METRES, closed_profile=True),
    )
    add_lathe(
        mesh_builder,
        HAT_DOME_PROFILE,
        ROUND_SEGMENT_COUNT,
        0,
        tiles_around=KNIT_TILES_AROUND_HAT,
        along_coordinates=profile_coordinates(HAT_DOME_PROFILE, KNIT_TILE_METRES),
    )

    pompom_centre = Vector((0.0, 0.0, POMPOM_CENTRE_HEIGHT))
    pompom_matrix = Matrix.Translation(pompom_centre) @ Matrix.Rotation(0.5, 4, "X")
    pompom_faces = add_primitive(mesh_builder, 0, lambda: bmesh.ops.create_icosphere(
        mesh_builder, subdivisions=2, radius=POMPOM_RADIUS, matrix=pompom_matrix))
    for face in pompom_faces:
        uvs = []
        for vertex in face.verts:
            from_centre = vertex.co - pompom_centre
            around = (math.atan2(from_centre.y, from_centre.x) / (2 * math.pi)) * KNIT_TILES_AROUND_POMPOM
            uvs.append([around, from_centre.z / KNIT_TILE_METRES])
        highest_around = max(uv[0] for uv in uvs)
        for uv in uvs:
            if highest_around - uv[0] > KNIT_TILES_AROUND_POMPOM / 2:
                uv[0] += KNIT_TILES_AROUND_POMPOM
        set_face_uvs(mesh_builder, face, uvs)

    head_pivot = Matrix.Translation((0.0, 0.0, HEAD_CENTRE_HEIGHT))
    tilt_back = head_pivot @ Matrix.Rotation(math.radians(-HAT_TILT_BACK_DEGREES), 4, "X") @ head_pivot.inverted()
    bmesh.ops.transform(mesh_builder, matrix=tilt_back, verts=mesh_builder.verts[:])
    return finish_object("Hat", mesh_builder, [player_colour], collection)


def build_scarf_wrap(collection, player_colour):
    mesh_builder = bmesh.new()
    profile = []
    for point_index in range(SCARF_PROFILE_POINT_COUNT):
        angle = 2 * math.pi * point_index / SCARF_PROFILE_POINT_COUNT - math.pi / 2
        profile.append((
            SCARF_MAJOR_RADIUS + SCARF_RADIAL_HALF_THICKNESS * math.cos(angle),
            SCARF_CENTRE_HEIGHT + SCARF_HALF_HEIGHT * math.sin(angle),
        ))
    add_lathe(
        mesh_builder,
        profile,
        ROUND_SEGMENT_COUNT,
        0,
        closed_profile=True,
        tiles_around=KNIT_TILES_AROUND_SCARF,
        along_coordinates=[
            point_index / SCARF_PROFILE_POINT_COUNT * KNIT_TILES_ACROSS_SCARF
            for point_index in range(SCARF_PROFILE_POINT_COUNT + 1)
        ],
        around_is_texture_vertical=True,
    )
    bmesh.ops.transform(mesh_builder, matrix=scarf_tilt_matrix(), verts=mesh_builder.verts[:])
    return finish_object("ScarfWrap", mesh_builder, [player_colour], collection)


def scarf_tail_directions():
    azimuth = math.radians(SCARF_TAIL_AZIMUTH_DEGREES)
    outward = Vector((math.sin(azimuth), -math.cos(azimuth), 0.0))
    sideways = Vector((math.cos(azimuth), math.sin(azimuth), 0.0))
    return outward, sideways


def scarf_tilt_matrix():
    _, sideways = scarf_tail_directions()
    pivot = Matrix.Translation((0.0, 0.0, SCARF_CENTRE_HEIGHT))
    return pivot @ Matrix.Rotation(math.radians(SCARF_TILT_DEGREES), 4, sideways) @ pivot.inverted()


def build_scarf_tail(collection, player_colour):
    mesh_builder = bmesh.new()
    outward, sideways = scarf_tail_directions()
    wrap_outer_radius = SCARF_MAJOR_RADIUS + SCARF_RADIAL_HALF_THICKNESS
    half_stitch_in_tiles = 0.125

    rings = []
    for ring_settings in SCARF_TAIL_RINGS:
        height = ring_settings["height"]
        half_width = ring_settings["width"] / 2
        thickness = SCARF_TAIL_THICKNESS * ring_settings["thickness_factor"]
        inner_distance = body_radius_at(height) + 0.004
        if height > SCARF_CENTRE_HEIGHT - SCARF_HALF_HEIGHT:
            inner_distance = wrap_outer_radius - 0.014
        cross_section = [
            (half_width - SCARF_TAIL_CHAMFER, 0.0),
            (half_width, thickness / 2),
            (half_width - SCARF_TAIL_CHAMFER, thickness),
            (-half_width + SCARF_TAIL_CHAMFER, thickness),
            (-half_width, thickness / 2),
            (-half_width + SCARF_TAIL_CHAMFER, 0.0),
        ]
        vertices = []
        for sideways_offset, outward_offset in cross_section:
            position = (
                outward * (inner_distance + outward_offset)
                + sideways * (sideways_offset + ring_settings["sideways_shift"])
            )
            vertices.append(mesh_builder.verts.new((position.x, position.y, height)))

        distances = [0.0]
        for corner, next_corner in zip(cross_section, cross_section[1:] + cross_section[:1]):
            distances.append(distances[-1] + math.dist(corner, next_corner))
        front_centre_distance = (distances[2] + distances[3]) / 2
        across_coordinates = [
            (distance - front_centre_distance) / KNIT_TILE_METRES + half_stitch_in_tiles
            for distance in distances
        ]
        rings.append({
            "vertices": vertices,
            "across": across_coordinates,
            "along": height / KNIT_TILE_METRES,
            "cross_section": cross_section,
        })

    corner_count = len(rings[0]["vertices"])
    for upper, lower in zip(rings, rings[1:]):
        for corner_index in range(corner_count):
            next_corner = (corner_index + 1) % corner_count
            face = mesh_builder.faces.new((
                upper["vertices"][corner_index],
                upper["vertices"][next_corner],
                lower["vertices"][next_corner],
                lower["vertices"][corner_index],
            ))
            set_face_uvs(mesh_builder, face, [
                (upper["across"][corner_index], upper["along"]),
                (upper["across"][corner_index + 1], upper["along"]),
                (lower["across"][corner_index + 1], lower["along"]),
                (lower["across"][corner_index], lower["along"]),
            ])
    for ring, corner_order in ((rings[0], range(corner_count)), (rings[-1], reversed(range(corner_count)))):
        ordered = list(corner_order)
        face = mesh_builder.faces.new([ring["vertices"][corner_index] for corner_index in ordered])
        set_face_uvs(mesh_builder, face, [
            (
                ring["cross_section"][corner_index][0] / KNIT_TILE_METRES + half_stitch_in_tiles,
                ring["cross_section"][corner_index][1] / KNIT_TILE_METRES,
            )
            for corner_index in ordered
        ])
    bmesh.ops.recalc_face_normals(mesh_builder, faces=mesh_builder.faces[:])
    return finish_object("ScarfTail", mesh_builder, [player_colour], collection)


def build_scarf_anchor(collection):
    outward, _ = scarf_tail_directions()
    anchor = bpy.data.objects.new("ScarfAnchor", None)
    anchor.empty_display_type = "PLAIN_AXES"
    anchor.empty_display_size = 0.08
    anchor_position = outward * (SCARF_MAJOR_RADIUS + SCARF_RADIAL_HALF_THICKNESS)
    anchor_position.z = SCARF_CENTRE_HEIGHT
    anchor.location = scarf_tilt_matrix() @ anchor_position
    collection.objects.link(anchor)
    return anchor


def triangle_count(mesh_object):
    return sum(len(polygon.vertices) - 2 for polygon in mesh_object.data.polygons)


def print_report(player_objects):
    mesh_objects = [player_object for player_object in player_objects if player_object.type == "MESH"]
    total = 0
    for mesh_object in mesh_objects:
        count = triangle_count(mesh_object)
        total += count
        print(f"REPORT triangles {mesh_object.name}: {count}")
    print(f"REPORT triangles total: {total}")

    world_corners = [
        mesh_object.matrix_world @ Vector(corner)
        for mesh_object in mesh_objects
        for corner in mesh_object.bound_box
    ]
    minimum = [min(corner[axis] for corner in world_corners) for axis in range(3)]
    maximum = [max(corner[axis] for corner in world_corners) for axis in range(3)]
    size = [round(maximum[axis] - minimum[axis], 3) for axis in range(3)]
    print(f"REPORT dimensions x/y/z: {size}, lowest z {round(minimum[2], 3)}, highest z {round(maximum[2], 3)}")


def export_fbx(player_objects):
    for scene_object in bpy.context.scene.objects:
        scene_object.select_set(False)
    for player_object in player_objects:
        player_object.select_set(True)
    bpy.context.view_layer.objects.active = player_objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_DIRECTORY / "Player.fbx"),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        axis_forward="-Z",
        axis_up="Y",
        apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        bake_anim=False,
        add_leaf_bones=False,
    )


def build_stage(stage_collection):
    stage_material = create_material("PreviewStage", STAGE_HEX, roughness=1.0)
    mesh_builder = bmesh.new()
    curve_radius = 2.5
    curve_start_depth = 2.0
    cross_section = [(-14.0, 0.0), (curve_start_depth, 0.0)]
    curve_step_count = 10
    for step_index in range(1, curve_step_count + 1):
        angle = math.pi / 2 * step_index / curve_step_count
        cross_section.append((
            curve_start_depth + curve_radius * math.sin(angle),
            curve_radius - curve_radius * math.cos(angle),
        ))
    cross_section.append((curve_start_depth + curve_radius, 16.0))
    left_vertices = [mesh_builder.verts.new((-24.0, depth, height)) for depth, height in cross_section]
    right_vertices = [mesh_builder.verts.new((24.0, depth, height)) for depth, height in cross_section]
    for index in range(len(cross_section) - 1):
        mesh_builder.faces.new((
            left_vertices[index], right_vertices[index], right_vertices[index + 1], left_vertices[index + 1]))
    stage_mesh = bpy.data.meshes.new("PreviewStage")
    mesh_builder.to_mesh(stage_mesh)
    mesh_builder.free()
    stage_mesh.materials.append(stage_material)
    for polygon in stage_mesh.polygons:
        polygon.use_smooth = True
    stage_collection.objects.link(bpy.data.objects.new("PreviewStage", stage_mesh))

    def add_area_light(name, location, energy, size, casts_shadow, colour):
        light_data = bpy.data.lights.new(name, "AREA")
        light_data.energy = energy
        light_data.size = size
        light_data.color = colour
        light_data.use_shadow = casts_shadow
        light_object = bpy.data.objects.new(name, light_data)
        light_object.location = location
        direction = Vector((0.0, 0.0, 0.45)) - Vector(location)
        light_object.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        stage_collection.objects.link(light_object)

    add_area_light("PreviewKey", (2.6, -3.2, 4.6), 300, 2.2, True, (1.0, 0.98, 0.95))
    add_area_light("PreviewFill", (-4.0, -3.0, 2.2), 90, 6.0, False, (0.94, 0.96, 1.0))

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    background = next(node for node in world.node_tree.nodes if node.type == "BACKGROUND")
    background.inputs["Color"].default_value = hex_to_linear_rgba("#FFF6E8")
    background.inputs["Strength"].default_value = 0.6
    bpy.context.scene.world = world


def configure_render():
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    if hasattr(scene.eevee, "taa_render_samples"):
        scene.eevee.taa_render_samples = 64
    print(f"REPORT render engine: {scene.render.engine}")


def render_shot(file_name, stage_collection, target, distance, azimuth_degrees, elevation_degrees, lens=70):
    azimuth = math.radians(azimuth_degrees)
    elevation = math.radians(elevation_degrees)
    target_vector = Vector(target)
    location = target_vector + distance * Vector((
        math.sin(azimuth) * math.cos(elevation),
        -math.cos(azimuth) * math.cos(elevation),
        math.sin(elevation),
    ))
    camera_data = bpy.data.cameras.new(file_name)
    camera_data.lens = lens
    camera_object = bpy.data.objects.new(file_name, camera_data)
    camera_object.location = location
    camera_object.rotation_euler = (target_vector - location).to_track_quat("-Z", "Y").to_euler()
    stage_collection.objects.link(camera_object)

    scene = bpy.context.scene
    scene.camera = camera_object
    scene.render.filepath = str(OUTPUT_DIRECTORY / file_name)
    bpy.ops.render.render(write_still=True)


def create_colour_variants(player_objects, stage_collection):
    spacing = 0.68
    variants = []
    for variant_index, (colour_name, hex_colour) in enumerate(PLAYER_COLOUR_HEXES.items()):
        variant_material = create_knit_material(f"Preview{colour_name}", hex_colour)
        offset = (variant_index - (len(PLAYER_COLOUR_HEXES) - 1) / 2) * spacing
        for player_object in player_objects:
            if player_object.type != "MESH":
                continue
            variant = player_object.copy()
            variant.data = player_object.data.copy()
            variant.name = f"{player_object.name}{colour_name}"
            for slot_index, material in enumerate(variant.data.materials):
                if material.name == "PlayerColour":
                    variant.data.materials[slot_index] = variant_material
            variant.location.x += offset
            stage_collection.objects.link(variant)
            variants.append(variant)
    return variants


def set_render_visibility(scene_objects, visible):
    for scene_object in scene_objects:
        scene_object.hide_render = not visible


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene

    player_collection = bpy.data.collections.new("Player")
    scene.collection.children.link(player_collection)

    wood = create_wood_material("Wood")
    face_dark = create_material("FaceDark", FACE_DARK_HEX)
    carrot = create_material("Carrot", CARROT_HEX)
    player_colour = create_knit_material("PlayerColour", PLAYER_COLOUR_HEXES["Blue"])

    body = build_body(player_collection, wood, face_dark, carrot)
    hat = build_hat(player_collection, player_colour)
    scarf_wrap = build_scarf_wrap(player_collection, player_colour)
    scarf_tail = build_scarf_tail(player_collection, player_colour)
    scarf_anchor = build_scarf_anchor(player_collection)
    player_objects = [body, hat, scarf_wrap, scarf_tail, scarf_anchor]

    bpy.context.view_layer.update()
    print_report(player_objects)

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIRECTORY / "Player.blend"))
    for image in bpy.data.images:
        if image.filepath:
            image.filepath = bpy.path.relpath(image.filepath)
    bpy.ops.wm.save_mainfile()
    export_fbx(player_objects)

    stage_collection = bpy.data.collections.new("PreviewStage")
    scene.collection.children.link(stage_collection)
    build_stage(stage_collection)
    configure_render()
    variants = create_colour_variants(player_objects, stage_collection)

    set_render_visibility(variants, False)
    render_shot("preview-front.png", stage_collection, (0.0, 0.0, 0.52), 3.1, 22, 10)
    render_shot("preview-top.png", stage_collection, (0.0, 0.0, 0.45), 3.4, 0, 60)
    render_shot("preview-face.png", stage_collection, (0.0, 0.0, 0.75), 1.3, 12, 6)
    render_shot("preview-side.png", stage_collection, (0.0, 0.0, 0.52), 3.1, 90, 6)
    scarf_tail.hide_render = True
    render_shot("preview-no-tail.png", stage_collection, (0.0, 0.0, 0.52), 3.1, 22, 10)
    scarf_tail.hide_render = False

    set_render_visibility(player_objects, False)
    set_render_visibility(variants, True)
    render_shot("preview-colours.png", stage_collection, (0.0, 0.0, 0.52), 6.0, 0, 8)


main()
