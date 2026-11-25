import math
import pathlib
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = pathlib.Path(__file__).resolve().parent
TEXTURE_DIRECTORY = OUTPUT_DIRECTORY.parent / "textures"

KNIT_TILE_METRES = 0.19
KNIT_TILES_AROUND_SCARF = 10
KNIT_HALF_STITCH_IN_TILES = 0.125
KNIT_NORMAL_STRENGTH = 1.0

MATERIAL_SPECIFICATIONS = {
    "Snow": ("#F4F7FA", 0.6),
    "FaceDark": ("#4A3A33", 0.8),
    "Carrot": ("#F08A2C", 0.8),
    "Stick": ("#7A4E2D", 0.8),
    "PlayerColour": ("#3E5BA9", 0.8),
}
PREVIEW_PLAYER_COLOURS = {
    "Blue": "#3E5BA9",
    "Red": "#C9463D",
    "Green": "#4E9A5B",
    "Yellow": "#E8B63A",
}
BACKDROP_COLOUR = "#E3D0B1"

BALL_SUBDIVISIONS = 3
BALL_TANGENT_JITTER = 0.07
BALL_RADIAL_JITTER = 0.05

BODY_DIAMETER = 0.8
HEAD_DIAMETER = 0.56
HEAD_SINK = 0.08
BODY_CENTRE = Vector((0.0, 0.0, BODY_DIAMETER / 2))
HEAD_CENTRE = Vector((0.0, 0.0, BODY_DIAMETER - HEAD_SINK + HEAD_DIAMETER / 2))

SCARF_TAIL_AZIMUTH_DEGREES = 27.0


def linear_channel_from_srgb(channel):
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def linear_colour_from_hex(hex_colour):
    channels = [int(hex_colour[index:index + 2], 16) / 255 for index in (1, 3, 5)]
    return tuple(linear_channel_from_srgb(channel) for channel in channels) + (1.0,)


def create_material(name, hex_colour, roughness):
    material = bpy.data.materials.new(name)
    if material.node_tree is None:
        material.use_nodes = True
    node_tree = material.node_tree
    node_tree.nodes.clear()
    shader_node = node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    output_node = node_tree.nodes.new("ShaderNodeOutputMaterial")
    output_node.location = (300, 0)
    node_tree.links.new(shader_node.outputs["BSDF"], output_node.inputs["Surface"])
    colour = linear_colour_from_hex(hex_colour)
    shader_node.inputs["Base Color"].default_value = colour
    shader_node.inputs["Roughness"].default_value = roughness
    material.diffuse_color = colour
    material.roughness = roughness
    return material


def load_texture(file_name, is_colour):
    image = bpy.data.images.load(str(TEXTURE_DIRECTORY / file_name), check_existing=True)
    if not is_colour:
        image.colorspace_settings.name = "Non-Color"
    return image


def create_knit_material(name, hex_colour, roughness):
    material = create_material(name, hex_colour, roughness)
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    shader_node = next(node for node in nodes if node.type == "BSDF_PRINCIPLED")

    colour_node = nodes.new("ShaderNodeRGB")
    colour_node.name = "PlayerColour"
    colour_node.label = "PlayerColour"
    colour_node.outputs[0].default_value = linear_colour_from_hex(hex_colour)

    albedo_node = nodes.new("ShaderNodeTexImage")
    albedo_node.image = load_texture("knit-albedo.png", is_colour=True)
    multiply_node = nodes.new("ShaderNodeMix")
    multiply_node.data_type = "RGBA"
    multiply_node.blend_type = "MULTIPLY"
    factor_input = next(
        socket for socket in multiply_node.inputs if socket.name == "Factor" and socket.type == "VALUE"
    )
    factor_input.default_value = 1.0
    colour_inputs = [socket for socket in multiply_node.inputs if socket.type == "RGBA"]
    links.new(albedo_node.outputs["Color"], colour_inputs[0])
    links.new(colour_node.outputs[0], colour_inputs[1])
    multiplied_output = next(socket for socket in multiply_node.outputs if socket.type == "RGBA")
    links.new(multiplied_output, shader_node.inputs["Base Color"])

    normal_image_node = nodes.new("ShaderNodeTexImage")
    normal_image_node.image = load_texture("knit-normal.png", is_colour=False)
    normal_map_node = nodes.new("ShaderNodeNormalMap")
    normal_map_node.inputs["Strength"].default_value = KNIT_NORMAL_STRENGTH
    links.new(normal_image_node.outputs["Color"], normal_map_node.inputs["Color"])
    links.new(normal_map_node.outputs["Normal"], shader_node.inputs["Normal"])
    return material


def set_face_uvs(target_mesh, face, texture_coordinates):
    uv_layer = target_mesh.loops.layers.uv.verify()
    for loop, texture_coordinate in zip(face.loops, texture_coordinates):
        loop[uv_layer].uv = texture_coordinate


def create_collection(scene, name):
    collection = bpy.data.collections.new(name)
    scene.collection.children.link(collection)
    return collection


def direction_from_angles(azimuth_degrees, elevation_degrees):
    azimuth = math.radians(azimuth_degrees)
    elevation = math.radians(elevation_degrees)
    return Vector((
        math.sin(azimuth) * math.cos(elevation),
        -math.cos(azimuth) * math.cos(elevation),
        math.sin(elevation),
    ))


def build_ball(diameter, seed, centre):
    random_source = random.Random(seed)
    ball_mesh = bmesh.new()
    bmesh.ops.create_icosphere(ball_mesh, subdivisions=BALL_SUBDIVISIONS, radius=1.0)
    for vertex in ball_mesh.verts:
        direction = vertex.co.normalized()
        tangent_offset = Vector((
            random_source.uniform(-1.0, 1.0),
            random_source.uniform(-1.0, 1.0),
            random_source.uniform(-1.0, 1.0),
        ))
        tangent_offset -= direction * tangent_offset.dot(direction)
        direction = (direction + tangent_offset * BALL_TANGENT_JITTER).normalized()
        vertex.co = direction * (1.0 - random_source.random() * BALL_RADIAL_JITTER)
    largest_radius = max(vertex.co.length for vertex in ball_mesh.verts)
    scale_to_diameter = diameter / 2 / largest_radius
    for vertex in ball_mesh.verts:
        vertex.co = vertex.co * scale_to_diameter + centre
    return ball_mesh


def add_blob(target_mesh, centre, outward_direction, radius, flatten, subdivisions):
    orientation = outward_direction.to_track_quat("Z", "Y").to_matrix().to_4x4()
    squash = Matrix.Diagonal((1.0, 1.0, flatten, 1.0))
    bmesh.ops.create_icosphere(
        target_mesh,
        subdivisions=subdivisions,
        radius=radius,
        matrix=Matrix.Translation(centre) @ orientation @ squash,
    )


def add_button_disc(target_mesh, surface_point, outward_direction, radius, proud_height, rim_bevel):
    side_count = 12
    embedded_depth = 0.03
    orientation = outward_direction.to_track_quat("Z", "Y").to_matrix()
    profile = [
        (radius, -embedded_depth),
        (radius, proud_height - rim_bevel),
        (radius - rim_bevel * (1 - math.cos(math.radians(45))), proud_height - rim_bevel * (1 - math.sin(math.radians(45)))),
        (radius - rim_bevel, proud_height),
    ]
    rings = []
    for ring_radius, ring_height in profile:
        ring = []
        for side_index in range(side_count):
            angle = 2 * math.pi * side_index / side_count
            local_position = Vector((math.cos(angle) * ring_radius, math.sin(angle) * ring_radius, ring_height))
            ring.append(target_mesh.verts.new(surface_point + orientation @ local_position))
        rings.append(ring)
    for lower_ring, upper_ring in zip(rings, rings[1:]):
        for side_index in range(side_count):
            next_index = (side_index + 1) % side_count
            target_mesh.faces.new((
                lower_ring[side_index],
                lower_ring[next_index],
                upper_ring[next_index],
                upper_ring[side_index],
            ))
    target_mesh.faces.new(rings[-1])
    target_mesh.faces.new(list(reversed(rings[0])))


def add_tapered_stick(target_mesh, start, end, start_radius, end_radius, side_count):
    orientation = (end - start).normalized().to_track_quat("Z", "Y").to_matrix()
    start_ring = []
    end_ring = []
    for side_index in range(side_count):
        angle = 2 * math.pi * side_index / side_count
        radial = orientation @ Vector((math.cos(angle), math.sin(angle), 0.0))
        start_ring.append(target_mesh.verts.new(start + radial * start_radius))
        end_ring.append(target_mesh.verts.new(end + radial * end_radius))
    for side_index in range(side_count):
        next_index = (side_index + 1) % side_count
        target_mesh.faces.new((
            start_ring[side_index],
            start_ring[next_index],
            end_ring[next_index],
            end_ring[side_index],
        ))
    target_mesh.faces.new(end_ring)
    target_mesh.faces.new(list(reversed(start_ring)))


def add_scarf_ring(target_mesh, centre, major_radius, radial_half_size, vertical_half_size, tilt, seed):
    major_segment_count = 14
    profile_segment_count = 6
    random_source = random.Random(seed)
    profile_angles = [
        2 * math.pi * (profile_index + 0.5) / profile_segment_count
        for profile_index in range(profile_segment_count)
    ]
    profile_points = [
        (math.cos(profile_angle) * radial_half_size, math.sin(profile_angle) * vertical_half_size)
        for profile_angle in profile_angles
    ]
    hidden_seam_index = profile_segment_count // 2
    outer_face_start_index = profile_segment_count - 1
    distances_from_seam = [0.0]
    for step in range(profile_segment_count):
        profile_index = (hidden_seam_index + step) % profile_segment_count
        next_profile_index = (profile_index + 1) % profile_segment_count
        edge_length = math.dist(profile_points[profile_index], profile_points[next_profile_index])
        if profile_index == outer_face_start_index:
            outer_face_centre_distance = distances_from_seam[-1] + edge_length / 2
        distances_from_seam.append(distances_from_seam[-1] + edge_length)

    def across_coordinate(steps_from_seam):
        distance = distances_from_seam[steps_from_seam]
        return (distance - outer_face_centre_distance) / KNIT_TILE_METRES + KNIT_HALF_STITCH_IN_TILES

    rings = []
    for major_index in range(major_segment_count):
        major_angle = 2 * math.pi * major_index / major_segment_count
        outward = Vector((math.cos(major_angle), math.sin(major_angle), 0.0))
        bulge = 1.0 + random_source.uniform(-0.08, 0.08)
        ring = []
        for profile_angle in profile_angles:
            point = outward * (major_radius + math.cos(profile_angle) * radial_half_size * bulge)
            point.z = math.sin(profile_angle) * vertical_half_size * bulge
            ring.append(target_mesh.verts.new(centre + tilt @ point))
        rings.append(ring)
    for major_index in range(major_segment_count):
        next_major_index = (major_index + 1) % major_segment_count
        for profile_index in range(profile_segment_count):
            next_profile_index = (profile_index + 1) % profile_segment_count
            face = target_mesh.faces.new((
                rings[major_index][profile_index],
                rings[next_major_index][profile_index],
                rings[next_major_index][next_profile_index],
                rings[major_index][next_profile_index],
            ))
            steps_from_seam = (profile_index - hidden_seam_index) % profile_segment_count
            across_start = across_coordinate(steps_from_seam)
            across_end = across_coordinate(steps_from_seam + 1)
            around_start = major_index / major_segment_count * KNIT_TILES_AROUND_SCARF
            around_end = (major_index + 1) / major_segment_count * KNIT_TILES_AROUND_SCARF
            set_face_uvs(target_mesh, face, (
                (across_start, around_start),
                (across_start, around_end),
                (across_end, around_end),
                (across_end, around_start),
            ))


def add_scarf_knot(target_mesh, centre, outward_direction, radius, flatten):
    faces_before = set(target_mesh.faces)
    add_blob(target_mesh, centre, outward_direction, radius, flatten, 1)
    width_axis = Vector((-outward_direction.y, outward_direction.x, 0.0))
    perimeter = 2 * math.pi * radius * math.sqrt((1.0 + flatten * flatten) / 2)
    tiles_around = perimeter / KNIT_TILE_METRES
    for face in [face for face in target_mesh.faces if face not in faces_before]:
        texture_coordinates = []
        for loop in face.loops:
            from_centre = loop.vert.co - centre
            turn = math.atan2(from_centre.dot(width_axis), from_centre.dot(outward_direction)) / (2 * math.pi)
            texture_coordinates.append([
                turn * tiles_around + KNIT_HALF_STITCH_IN_TILES,
                loop.vert.co.z / KNIT_TILE_METRES,
            ])
        highest_across = max(texture_coordinate[0] for texture_coordinate in texture_coordinates)
        for texture_coordinate in texture_coordinates:
            if highest_across - texture_coordinate[0] > tiles_around / 2:
                texture_coordinate[0] += tiles_around
        set_face_uvs(target_mesh, face, texture_coordinates)


def build_scarf_tail(path_stations):
    tail_mesh = bmesh.new()
    radial_axis = direction_from_angles(SCARF_TAIL_AZIMUTH_DEGREES, 0.0)
    width_axis = Vector((radial_axis.y, -radial_axis.x, 0.0)) * -1.0
    centres = [
        radial_axis * station["radius"] + width_axis * station["sway"] + Vector((0.0, 0.0, station["height"]))
        for station in path_stations
    ]
    rings = []
    ring_across_coordinates = []
    ring_along_coordinates = []
    walked_length = 0.0
    for station_index, station in enumerate(path_stations):
        if station_index > 0:
            walked_length += (centres[station_index] - centres[station_index - 1]).length
        ring_along_coordinates.append(-walked_length / KNIT_TILE_METRES)
        unrolled_distances = [
            0.0,
            station["width"],
            station["width"] + station["thickness"],
            2 * station["width"] + station["thickness"],
            2 * station["width"] + 2 * station["thickness"],
        ]
        front_centre_distance = 1.5 * station["width"] + station["thickness"]
        ring_across_coordinates.append([
            (distance - front_centre_distance) / KNIT_TILE_METRES + KNIT_HALF_STITCH_IN_TILES
            for distance in unrolled_distances
        ])
        previous_centre = centres[max(station_index - 1, 0)]
        next_centre = centres[min(station_index + 1, len(centres) - 1)]
        tangent = (next_centre - previous_centre).normalized()
        surface_normal = width_axis.cross(tangent).normalized()
        if surface_normal.dot(radial_axis) < 0:
            surface_normal = -surface_normal
        half_width = width_axis * station["width"] / 2
        half_thickness = surface_normal * station["thickness"] / 2
        centre = centres[station_index]
        rings.append([
            tail_mesh.verts.new(centre - half_width - half_thickness),
            tail_mesh.verts.new(centre + half_width - half_thickness),
            tail_mesh.verts.new(centre + half_width + half_thickness),
            tail_mesh.verts.new(centre - half_width + half_thickness),
        ])
    for station_index in range(len(rings) - 1):
        for corner_index in range(4):
            next_corner_index = (corner_index + 1) % 4
            face = tail_mesh.faces.new((
                rings[station_index][corner_index],
                rings[station_index][next_corner_index],
                rings[station_index + 1][next_corner_index],
                rings[station_index + 1][corner_index],
            ))
            set_face_uvs(tail_mesh, face, (
                (ring_across_coordinates[station_index][corner_index], ring_along_coordinates[station_index]),
                (ring_across_coordinates[station_index][corner_index + 1], ring_along_coordinates[station_index]),
                (ring_across_coordinates[station_index + 1][corner_index + 1], ring_along_coordinates[station_index + 1]),
                (ring_across_coordinates[station_index + 1][corner_index], ring_along_coordinates[station_index + 1]),
            ))
    for station_index, corner_order in ((0, (0, 1, 2, 3)), (len(rings) - 1, (3, 2, 1, 0))):
        station = path_stations[station_index]
        face = tail_mesh.faces.new([rings[station_index][corner_index] for corner_index in corner_order])
        cap_corners = (
            (-station["width"] / 2, -station["thickness"] / 2),
            (station["width"] / 2, -station["thickness"] / 2),
            (station["width"] / 2, station["thickness"] / 2),
            (-station["width"] / 2, station["thickness"] / 2),
        )
        set_face_uvs(tail_mesh, face, [
            (
                cap_corners[corner_index][0] / KNIT_TILE_METRES + KNIT_HALF_STITCH_IN_TILES,
                cap_corners[corner_index][1] / KNIT_TILE_METRES,
            )
            for corner_index in corner_order
        ])
    return tail_mesh


def build_arm(side_sign, lift_degrees, forward_degrees, twig_fractions):
    arm_mesh = bmesh.new()
    shoulder_direction = Vector((side_sign * math.cos(math.radians(30)), 0.0, math.sin(math.radians(30))))
    shoulder = BODY_CENTRE + shoulder_direction * (BODY_DIAMETER / 2 - 0.07)
    lift = math.radians(lift_degrees)
    forward = math.radians(forward_degrees)
    arm_direction = Vector((
        side_sign * math.cos(lift) * math.cos(forward),
        -math.sin(forward) * math.cos(lift),
        math.sin(lift),
    ))
    arm_length = 0.34
    hand = shoulder + arm_direction * arm_length
    add_tapered_stick(arm_mesh, shoulder, hand, 0.034, 0.019, 5)
    upper_twig_direction = (arm_direction + Vector((-side_sign * 0.25, 0.0, 0.95))).normalized()
    lower_twig_direction = (arm_direction + Vector((side_sign * 0.55, -0.25, -0.5))).normalized()
    upper_twig_start = shoulder + arm_direction * arm_length * twig_fractions[0]
    lower_twig_start = shoulder + arm_direction * arm_length * twig_fractions[1]
    add_tapered_stick(arm_mesh, upper_twig_start, upper_twig_start + upper_twig_direction * 0.12, 0.02, 0.012, 4)
    add_tapered_stick(arm_mesh, lower_twig_start, lower_twig_start + lower_twig_direction * 0.095, 0.019, 0.012, 4)
    return arm_mesh


def create_object_from_mesh(name, source_mesh, material, collection):
    bmesh.ops.recalc_face_normals(source_mesh, faces=source_mesh.faces)
    for face in source_mesh.faces:
        face.smooth = False
    minimum_corner = Vector([min(vertex.co[axis] for vertex in source_mesh.verts) for axis in range(3)])
    maximum_corner = Vector([max(vertex.co[axis] for vertex in source_mesh.verts) for axis in range(3)])
    bounds_centre = (minimum_corner + maximum_corner) / 2
    for vertex in source_mesh.verts:
        vertex.co -= bounds_centre
    mesh_data = bpy.data.meshes.new(name)
    source_mesh.to_mesh(mesh_data)
    source_mesh.free()
    mesh_data.materials.append(material)
    scene_object = bpy.data.objects.new(name, mesh_data)
    scene_object.location = bounds_centre
    collection.objects.link(scene_object)
    return scene_object


def build_snowball(collection, materials):
    ball_mesh = build_ball(1.0, seed=11, centre=Vector((0.0, 0.0, 0.0)))
    mesh_data = bpy.data.meshes.new("Snowball")
    ball_mesh.to_mesh(mesh_data)
    ball_mesh.free()
    mesh_data.materials.append(materials["Snow"])
    snowball = bpy.data.objects.new("Snowball", mesh_data)
    collection.objects.link(snowball)
    return snowball


def build_snowman(collection, materials):
    body_mesh = build_ball(BODY_DIAMETER, seed=23, centre=BODY_CENTRE)
    lowest_point = min(vertex.co.z for vertex in body_mesh.verts)
    for vertex in body_mesh.verts:
        vertex.co.z -= lowest_point
    create_object_from_mesh("BodyBall", body_mesh, materials["Snow"], collection)

    head_mesh = build_ball(HEAD_DIAMETER, seed=37, centre=HEAD_CENTRE)
    create_object_from_mesh("HeadBall", head_mesh, materials["Snow"], collection)

    head_radius = HEAD_DIAMETER / 2
    body_radius = BODY_DIAMETER / 2

    nose_mesh = bmesh.new()
    nose_direction = direction_from_angles(0.0, 4.0)
    nose_base = HEAD_CENTRE + direction_from_angles(0.0, -3.0) * (head_radius - 0.05)
    add_tapered_stick(nose_mesh, nose_base, nose_base + nose_direction * 0.185, 0.055, 0.008, 7)
    create_object_from_mesh("Nose", nose_mesh, materials["Carrot"], collection)

    eyes_mesh = bmesh.new()
    for azimuth_degrees in (-21.0, 21.0):
        eye_direction = direction_from_angles(azimuth_degrees, 17.0)
        add_button_disc(eyes_mesh, HEAD_CENTRE + eye_direction * head_radius, eye_direction, 0.028, 0.008, 0.007)
    create_object_from_mesh("Eyes", eyes_mesh, materials["FaceDark"], collection)

    buttons_mesh = bmesh.new()
    for azimuth_degrees, elevation_degrees in ((-3.0, 33.0), (-1.0, 8.0)):
        button_direction = direction_from_angles(azimuth_degrees, elevation_degrees)
        add_button_disc(buttons_mesh, BODY_CENTRE + button_direction * body_radius, button_direction, 0.036, 0.008, 0.008)
    create_object_from_mesh("Buttons", buttons_mesh, materials["FaceDark"], collection)

    create_object_from_mesh("ArmLeft", build_arm(1.0, 40.0, 8.0, (0.6, 0.74)), materials["Stick"], collection)
    create_object_from_mesh("ArmRight", build_arm(-1.0, 34.0, 12.0, (0.66, 0.56)), materials["Stick"], collection)

    wrap_mesh = bmesh.new()
    wrap_tilt = Matrix.Rotation(math.radians(5.0), 3, "Y") @ Matrix.Rotation(math.radians(-4.0), 3, "X")
    add_scarf_ring(wrap_mesh, Vector((0.0, 0.0, 0.775)), 0.222, 0.082, 0.092, wrap_tilt, seed=5)
    knot_direction = direction_from_angles(SCARF_TAIL_AZIMUTH_DEGREES, 0.0)
    add_scarf_knot(wrap_mesh, knot_direction * 0.3 + Vector((0.0, 0.0, 0.75)), knot_direction, 0.085, 0.7)
    create_object_from_mesh("ScarfWrap", wrap_mesh, materials["PlayerColour"], collection)

    tail_stations = [
        {"radius": 0.25, "height": 0.77, "sway": 0.0, "width": 0.11, "thickness": 0.05},
        {"radius": 0.318, "height": 0.69, "sway": 0.012, "width": 0.115, "thickness": 0.046},
        {"radius": 0.376, "height": 0.6, "sway": 0.03, "width": 0.124, "thickness": 0.044},
        {"radius": 0.416, "height": 0.5, "sway": 0.036, "width": 0.138, "thickness": 0.044},
        {"radius": 0.447, "height": 0.41, "sway": 0.024, "width": 0.156, "thickness": 0.044},
    ]
    create_object_from_mesh("ScarfTail", build_scarf_tail(tail_stations), materials["PlayerColour"], collection)


def export_collection_as_fbx(scene, collection, file_path):
    for scene_object in scene.objects:
        scene_object.select_set(False)
    for scene_object in collection.objects:
        scene_object.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(file_path),
        use_selection=True,
        object_types={"MESH"},
        axis_forward="-Z",
        axis_up="Y",
        apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        bake_anim=False,
        add_leaf_bones=False,
    )


def print_statistics(collection):
    total_triangles = 0
    minimum_corner = Vector((math.inf, math.inf, math.inf))
    maximum_corner = Vector((-math.inf, -math.inf, -math.inf))
    for scene_object in collection.objects:
        scene_object.data.calc_loop_triangles()
        triangle_count = len(scene_object.data.loop_triangles)
        total_triangles += triangle_count
        for vertex in scene_object.data.vertices:
            world_position = scene_object.matrix_world @ vertex.co
            for axis in range(3):
                minimum_corner[axis] = min(minimum_corner[axis], world_position[axis])
                maximum_corner[axis] = max(maximum_corner[axis], world_position[axis])
        print(f"STATS {collection.name}/{scene_object.name}: {triangle_count} triangles")
    size = maximum_corner - minimum_corner
    print(f"STATS {collection.name} total: {total_triangles} triangles, "
          f"size x={size.x:.3f} y={size.y:.3f} z={size.z:.3f}, "
          f"lowest z={minimum_corner.z:.3f}, highest z={maximum_corner.z:.3f}")


def aim_object(scene_object, location, target):
    scene_object.location = location
    scene_object.rotation_euler = (target - location).to_track_quat("-Z", "Y").to_euler()


def build_preview_stage(scene):
    stage_collection = create_collection(scene, "PreviewStage")

    backdrop_mesh = bmesh.new()
    profile = [(-14.0, 0.0), (3.0, 0.0)]
    sweep_radius = 3.0
    sweep_segment_count = 16
    for segment_index in range(1, sweep_segment_count + 1):
        angle = math.radians(-90.0 + 90.0 * segment_index / sweep_segment_count)
        profile.append((3.0 + math.cos(angle) * sweep_radius, sweep_radius + math.sin(angle) * sweep_radius))
    profile.append((3.0 + sweep_radius, 16.0))
    left_edge = [backdrop_mesh.verts.new((-22.0, depth, height)) for depth, height in profile]
    right_edge = [backdrop_mesh.verts.new((22.0, depth, height)) for depth, height in profile]
    for profile_index in range(len(profile) - 1):
        face = backdrop_mesh.faces.new((
            left_edge[profile_index],
            right_edge[profile_index],
            right_edge[profile_index + 1],
            left_edge[profile_index + 1],
        ))
        face.smooth = True
    backdrop_data = bpy.data.meshes.new("PreviewBackdrop")
    backdrop_mesh.to_mesh(backdrop_data)
    backdrop_mesh.free()
    backdrop_data.materials.append(create_material("PreviewBackdrop", BACKDROP_COLOUR, 0.9))
    stage_collection.objects.link(bpy.data.objects.new("PreviewBackdrop", backdrop_data))

    key_light_data = bpy.data.lights.new("PreviewKeyLight", "SUN")
    key_light_data.energy = 1.85
    key_light_data.angle = math.radians(14.0)
    key_light_data.color = (1.0, 0.97, 0.92)
    key_light = bpy.data.objects.new("PreviewKeyLight", key_light_data)
    stage_collection.objects.link(key_light)
    aim_object(key_light, Vector((-3.2, -4.0, 6.0)), Vector((0.0, 0.0, 0.0)))

    fill_light_data = bpy.data.lights.new("PreviewFillLight", "SUN")
    fill_light_data.energy = 0.55
    fill_light_data.angle = math.radians(40.0)
    fill_light_data.color = (0.9, 0.94, 1.0)
    fill_light_data.use_shadow = False
    fill_light = bpy.data.objects.new("PreviewFillLight", fill_light_data)
    stage_collection.objects.link(fill_light)
    aim_object(fill_light, Vector((5.0, -3.0, 2.5)), Vector((0.0, 0.0, 0.5)))

    world = bpy.data.worlds.new("PreviewWorld")
    if world.node_tree is None:
        world.use_nodes = True
    background_node = next(node for node in world.node_tree.nodes if node.type == "BACKGROUND")
    background_node.inputs["Color"].default_value = (0.86, 0.9, 1.0, 1.0)
    background_node.inputs["Strength"].default_value = 0.5
    scene.world = world

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera_data.lens = 85.0
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    stage_collection.objects.link(camera)
    scene.camera = camera

    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    for setting_name, setting_value in (("taa_render_samples", 64), ("use_shadows", True), ("use_raytracing", True)):
        if hasattr(scene.eevee, setting_name):
            setattr(scene.eevee, setting_name, setting_value)
    return camera


def clear_collection(collection):
    for scene_object in list(collection.objects):
        bpy.data.objects.remove(scene_object, do_unlink=True)


def place_preview_copies(source_collection, shot_collection, offset, uniform_scale, rotation_degrees, colour_material):
    placement = (
        Matrix.Translation(offset)
        @ Matrix.Rotation(math.radians(rotation_degrees), 4, "Z")
        @ Matrix.Scale(uniform_scale, 4)
    )
    for source_object in source_collection.objects:
        preview_copy = source_object.copy()
        preview_copy.data = source_object.data.copy()
        preview_copy.hide_render = False
        preview_copy.matrix_world = placement @ source_object.matrix_world
        if colour_material is not None:
            for slot_index, slot_material in enumerate(preview_copy.data.materials):
                if slot_material.name == "PlayerColour":
                    preview_copy.data.materials[slot_index] = colour_material
        shot_collection.objects.link(preview_copy)


def render_shot(scene, camera, file_name, target, azimuth_degrees, elevation_degrees, distance):
    aim_object(camera, target + direction_from_angles(azimuth_degrees, elevation_degrees) * distance, target)
    scene.render.filepath = str(OUTPUT_DIRECTORY / file_name)
    bpy.ops.render.render(write_still=True)


def render_previews(scene, snowman_collection, snowball_collection):
    for scene_object in list(snowman_collection.objects) + list(snowball_collection.objects):
        scene_object.hide_render = True
    camera = build_preview_stage(scene)
    shot_collection = create_collection(scene, "PreviewShot")
    no_offset = Vector((0.0, 0.0, 0.0))

    place_preview_copies(snowman_collection, shot_collection, no_offset, 1.0, 0.0, None)
    render_shot(scene, camera, "preview-snowman-front.png", Vector((0.0, 0.0, 0.66)), 14.0, 7.0, 4.3)
    render_shot(scene, camera, "preview-snowman-top.png", Vector((0.0, 0.0, 0.5)), 12.0, 60.0, 4.8)
    render_shot(scene, camera, "preview-snowman-scarf.png", Vector((0.06, 0.0, 0.66)), 22.0, 6.0, 1.75)
    render_shot(scene, camera, "preview-snowman-face.png", Vector((0.0, 0.0, 1.0)), 16.0, 5.0, 1.7)
    clear_collection(shot_collection)

    colour_spacing = 1.12
    for colour_index, (colour_name, hex_colour) in enumerate(PREVIEW_PLAYER_COLOURS.items()):
        colour_material = create_knit_material(f"PreviewPlayerColour{colour_name}", hex_colour, 0.8)
        offset = Vector(((colour_index - 1.5) * colour_spacing, 0.0, 0.0))
        place_preview_copies(snowman_collection, shot_collection, offset, 1.0, 0.0, colour_material)
    render_shot(scene, camera, "preview-snowman-colours.png", Vector((0.0, 0.0, 0.62)), 0.0, 7.0, 11.4)
    clear_collection(shot_collection)

    for diameter, horizontal_position, rotation_degrees in ((0.25, -0.72, 40.0), (0.5, -0.22, 115.0), (0.8, 0.55, 0.0)):
        offset = Vector((horizontal_position, 0.0, diameter / 2))
        place_preview_copies(snowball_collection, shot_collection, offset, diameter, rotation_degrees, None)
    render_shot(scene, camera, "preview-snowball.png", Vector((0.0, 0.0, 0.32)), 0.0, 14.0, 5.4)
    clear_collection(shot_collection)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    materials = {
        name: (create_knit_material if name == "PlayerColour" else create_material)(name, hex_colour, roughness)
        for name, (hex_colour, roughness) in MATERIAL_SPECIFICATIONS.items()
    }
    snowball_collection = create_collection(scene, "Snowball")
    snowman_collection = create_collection(scene, "Snowman")
    build_snowball(snowball_collection, materials)
    build_snowman(snowman_collection, materials)
    bpy.context.view_layer.update()

    print_statistics(snowball_collection)
    print_statistics(snowman_collection)

    export_collection_as_fbx(scene, snowball_collection, OUTPUT_DIRECTORY / "Snowball.fbx")
    export_collection_as_fbx(scene, snowman_collection, OUTPUT_DIRECTORY / "Snowman.fbx")

    bpy.context.view_layer.layer_collection.children["Snowball"].hide_viewport = True
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIRECTORY / "Snowman.blend"))
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_mainfile()

    render_previews(scene, snowman_collection, snowball_collection)


main()
