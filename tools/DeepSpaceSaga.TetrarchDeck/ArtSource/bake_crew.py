"""Bake the clothed body and a walk cycle from the Cesium Man GLB (see NOTICE).
Usage: python bake_crew.py CesiumMan.glb ../Art/crew-motion.bin
Requires NumPy. Coordinates in the output are forward, left and up.
"""
import argparse
import json
import struct
from pathlib import Path

import numpy as np


def rotation(q):
    x, y, z, w = q / np.linalg.norm(q)
    return np.array([
        [1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
        [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
        [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])


class WalkClip:
    def __init__(self, path):
        glb = path.read_bytes()
        size = struct.unpack_from("<I", glb, 12)[0]
        self.doc = json.loads(glb[20:20 + size])
        self.buffer = glb[28 + size:]
        skin = self.doc["skins"][0]
        self.joints = skin["joints"]
        self.parents = {child: i for i, node in enumerate(self.doc["nodes"])
                        for child in node.get("children", [])}
        self.inverse = self.read(skin["inverseBindMatrices"]).reshape(-1, 4, 4).transpose(0, 2, 1)
        self.bind = dict(zip(self.joints, np.linalg.inv(self.inverse)))
        animation = self.doc["animations"][0]
        self.tracks = {}
        for channel in animation["channels"]:
            sampler = animation["samplers"][channel["sampler"]]
            target = channel["target"]
            self.tracks[target["node"], target["path"]] = (self.read(sampler["input"])[:, 0], self.read(sampler["output"]))
        mesh = self.doc["meshes"][0]["primitives"][0]
        attr = mesh["attributes"]
        self.rest = self.read(attr["POSITION"])
        self.indices = self.read(mesh["indices"]).reshape(-1, 3)
        self.weights = self.read(attr["WEIGHTS_0"])
        self.bones = self.read(attr["JOINTS_0"])

    def read(self, index):
        a = self.doc["accessors"][index]
        view = self.doc["bufferViews"][a["bufferView"]]
        dims = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[a["type"]]
        dtype = np.dtype({5126: "<f4", 5123: "<u2", 5121: "u1"}[a["componentType"]])
        offset = view.get("byteOffset", 0) + a.get("byteOffset", 0)
        return np.ndarray((a["count"], dims), dtype, self.buffer, offset,
                          strides=(view.get("byteStride", dims * dtype.itemsize), dtype.itemsize)).copy()

    def sample(self, bone, key, time):
        times, values = self.tracks[bone, key]
        if time is None:
            samples = values[:-1]
            if key == "rotation":
                samples = np.where((samples @ samples[0])[:, None] < 0, -samples, samples)
            return samples.mean(axis=0)
        i = int(np.clip(np.searchsorted(times, time, side="right") - 1, 0, len(times) - 2))
        fraction = (time - times[i]) / (times[i+1] - times[i])
        a, b = values[i:i+2]
        if key == "rotation" and a @ b < 0:
            b = -b
        return a + fraction * (b - a)

    def matrices(self, time):
        world = {}
        for bone in sorted(self.joints):
            parent = self.parents[bone]
            # The bind pose has flat, evenly spaced boots. Relax the arms down
            # from their outstretched bind position using the clip's mean rotation.
            if time is None and not 14 <= bone <= 19:
                local = np.linalg.inv(self.bind.get(parent, np.eye(4))) @ self.bind[bone]
            else:
                local = np.eye(4)
                local[:3, 3] = self.sample(bone, "translation", time)
                orientation = self.sample(bone, "rotation", time)
                if time is not None and 14 <= bone <= 19:
                    relaxed = self.sample(bone, "rotation", None)
                    if relaxed @ orientation < 0:
                        orientation = -orientation
                    orientation = .28 * relaxed + .72 * orientation
                local[:3, :3] = rotation(orientation)
            world[bone] = world.get(parent, np.eye(4)) @ local
        return np.array([world[j] for j in self.joints]), world[20][:3, 3]


# A small triangle texture keeps cuffs, seams, piping and cabinet lighting
# readable at game scale. It is sampled in bind space, so details follow cloth.
PALETTE = np.array([
    [62,73,83], [78,96,105], [37,49,60], [39,41,43], [91,78,61],
    [188,157,115], [36,48,52], [150,169,174], [173,128,97], [53,97,104],
    [102,129,131], [208,159,70], [54,60,63], [84,83,76], [190,148,121],
], dtype=float)


def fabric(points):
    x, y, z = points.T
    side = np.abs(y)
    colors = np.zeros(len(z), dtype=int)
    jacket = z > .72
    colors[jacket] = 1
    colors[(z > .69) & (z < .745)] = 3
    colors[(z > .705) & (z < .731) & (x > .085) & (side < .025)] = 7
    colors[(z < .18)] = 3
    colors[(z < .029)] = 6
    colors[(z > .143) & (z < .157)] = 13
    colors[(z > .275) & (z < .411) & (x > .064)] = 12
    colors[(z > .385) & (z < .402) & (x > .064)] = 13
    colors[(z > .47) & (z < .61) & (side > .093)] = 2
    colors[(z > .578) & (z < .60) & (side > .095)] = 13
    colors[(z > .72) & (z < .76) & (side > .08)] = 4
    colors[(z > .746) & (z < 1.09) & (side < .007) & (x > .04)] = 6
    colors[(z > .88) & (z < .973) & (y > .021) & (y < .084) & (x > .055)] = 9
    colors[(z > .967) & (z < .978) & (y > .024) & (y < .079) & (x > .055)] = 7
    colors[(z > 1.017) & (side > .075) & (side < .22)] = 9
    colors[(z > 1.025) & (z < 1.052) & (side > .138)] = 11
    colors[(z > 1.081) & (side <= .105)] = 2
    colors[(z > 1.123) & (side < .09)] = 6
    colors[(side > .387) & (side <= .443)] = 2
    colors[(side > .425) & (side < .436)] = 10
    colors[side > .445] = 14
    weave = np.cos(z*157 + 1.9*np.sin(y*44)) + .6*np.sin(x*320 + z*204)
    shade = np.clip(np.round(3.5 + weave*1.55), 0, 7).astype(int)
    return (colors * 8 + shade).astype("u1")


def bake(source, target):
    clip = WalkClip(source)
    # Crop the source helmet. The runtime mounts the crew member's own DSS head here.
    triangles = clip.indices[np.max(clip.rest[clip.indices, 2], axis=1) < 1.164]
    chosen, inverse = np.unique(triangles, return_inverse=True)
    indices = inverse.reshape(-1, 3)
    points = clip.rest[chosen]
    rig = clip.bones[chosen]
    weights = clip.weights[chosen]
    local = np.c_[points, np.ones(len(points))]
    sample_count, texels = 40, 16
    palette = np.clip(PALETTE[:, None] * np.linspace(.91, 1.09, 8)[None, :, None], 0, 255).astype("u1")
    uv = np.indices((texels, texels))[::-1].reshape(2, -1).T / (texels - 1)
    bary = np.c_[1 - uv.sum(1), uv]
    skin = np.array([fabric(bary @ points[face]) for face in indices])
    groups = np.unique(np.round(points, 5), axis=0, return_inverse=True)[1].reshape(-1)

    def sample(time):
        mats, neck = clip.matrices(time)
        palettes = (mats @ clip.inverse)[rig]
        vertices = np.einsum("nvij,nv,nj->ni", palettes, weights, local)[:, :3]
        return np.vstack((vertices, neck))

    idle = sample(None)
    poses = []
    for t in np.arange(sample_count) * (2 / sample_count):
        posed = sample(t)
        # Bring the long source stride down to a walk inside a narrow ship.
        posed[:, 0] = idle[:, 0] + .68 * (posed[:, 0] - idle[:, 0])
        posed[:, 2] = idle[:, 2] + .73 * (posed[:, 2] - idle[:, 2])
        posed[:, 2] -= posed[:-1, 2].min()
        poses.append(posed)
    idle[:, 2] -= idle[:-1, 2].min()
    # Recover horizontal root travel from the sole in contact. Redistribute it
    # over equal distance samples; the planted boot then stays on its floor tile
    # as the simulation advances the character at its actual movement speed.
    soles = points[:, 2] < .035
    travel = []
    for a, b in zip(poses, poses[1:] + poses[:1]):
        elevation = np.maximum(a[:-1, 2], b[:-1, 2])
        feet = [soles & (points[:, 1] * sign > 0) for sign in (-1, 1)]
        support = min(feet, key=lambda foot: np.partition(elevation[foot], 4)[4])
        contact = support & (elevation <= np.partition(elevation[support], 4)[4] + .008)
        travel.append(np.median(a[:-1, :2][contact] - b[:-1, :2][contact], axis=0))
    travel = np.array(travel)
    offsets = np.vstack(([0, 0], np.cumsum(travel - travel.mean(axis=0), axis=0)[:-1]))
    for posed, offset in zip(poses, offsets - offsets.mean(axis=0)):
        posed[:, :2] += offset
    stride_tiles = float(travel[:, 0].sum() * 2 / 1.34)
    with target.open("wb") as out:
        out.write(b"DSSWALK1")
        out.write(struct.pack("<6if", len(points), len(indices), sample_count, texels,
                              palette.size // 3, groups.max() + 1, stride_tiles))
        out.write(indices.astype("<u2").tobytes())
        out.write(groups.astype("<u2").tobytes())
        out.write(palette.tobytes())
        out.write(skin.tobytes())
        for pose in [idle, *poses]:
            out.write(np.rint(pose * 10000).astype("<i2").tobytes())
    print(f"{len(points)} vertices, {len(indices)} triangles, {sample_count} poses, {stride_tiles:.3f} tiles per stride")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path)
    args = parser.parse_args()
    bake(args.source, args.target)
