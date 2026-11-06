---
license: cc0-1.0
pretty_name: Smithsonian Open Access 3D
source_datasets:
- original
task_categories:
- image-to-3d
- text-to-3d
tags:
- 3d
- mesh
- glb
- smithsonian
- museum
- cultural-heritage
- natural-history
- open-access
size_categories:
- 1K<n<10K
configs:
- config_name: default
  data_files:
  - split: train
    path: data/train-*.parquet
---

# Smithsonian Open Access 3D

A Hugging Face packaging of the 3D models in [Smithsonian Open Access](https://www.si.edu/openaccess):
scans of museum objects from the National Museum of Natural History (shells, corals, primate skulls,
fossils, plants, insects), the National Museum of African American History and Culture, the National
Museum of American History, the National Air and Space Museum, the National Museum of Asian Art, the
National Portrait Gallery, the Smithsonian American Art Museum and others.

Only items whose 3D media is marked **CC0** in the Open Access metadata are included. Every mesh is the
original Smithsonian GLB, stored byte-for-byte.

- Rows: **3,451** (one per 3D package; a package is one Voyager scan, and an object can have several)
- Mesh data: **22.3 GB** of GLB
- License: **CC0 1.0** (per-item `media_usage_access` is `CC0` for every row)

## Usage

```python
from datasets import load_dataset

ds = load_dataset("suvadityamuk/smithsonian-open-access-3d", split="train")
row = ds[0]
row["title"], row["unit_code"], row["scientific_name"]
scene = row["mesh"]                      # trimesh.Scene (pip install trimesh DracoPy)
row["preview"]                           # PIL.Image render of the scan
```

For the raw GLB bytes use `ds.cast_column("mesh", Mesh(decode=False))`, which yields
`{"bytes": <original GLB bytes>, "path": <file name>}`.

## Fields

| Field | Description |
| --- | --- |
| `package_id` | Smithsonian 3D package UUID (`3d_package:<id>`) |
| `title`, `package_title` | Object title from the Open Access record, and the 3D package title |
| `mesh` | Original GLB as a `Mesh` feature (see `mesh_source`) |
| `preview` | Smithsonian render of the scan (JPEG) |
| `unit_code`, `data_source` | Owning museum / department |
| `record_id`, `record_ids`, `record_link`, `guid` | Open Access record identifiers and persistent link |
| `object_type`, `topic`, `date`, `place`, `culture`, `set_name` | Indexed descriptive metadata |
| `tax_*`, `scientific_name` | Taxonomy for natural-history specimens |
| `license`, `media_usage_access` | `CC0-1.0` / the per-item Open Access usage flag |
| `voyager_url` | Interactive Voyager viewer for the scan |
| `mesh_source`, `mesh_url`, `draco_compressed` | Which Smithsonian file was used (see below) |
| `vertices`, `faces`, `gltf_textures`, `stats_json` | Mesh statistics computed with trimesh |
| `available_files_json` | Every file the Smithsonian publishes for the package (other resolutions, OBJ/PLY, USDZ) |
| `record_json` | The full, unmodified Open Access metadata record |

### Which mesh is stored

For each package the first available of these is used, all original Smithsonian files:

1. `web_high_nondraco`: the web "High" GLB without Draco compression (150k faces, 4096 px textures);
2. `download_low_resolution`: the downloadable "Low resolution" GLB, when it is not Draco-compressed;
3. `web_high`: the web "High" GLB with Draco mesh compression (`draco_compressed = true`).

Draco-compressed GLBs decode with `trimesh` only when `DracoPy` is installed; without it trimesh returns
placeholder zero geometry. Full-resolution scans (PLY and OBJ archives, about 0.8 TB in total) are not
included; their URLs are in `available_files_json`.

## License and terms

The Smithsonian dedicates Open Access items marked CC0 to the public domain: "This Content is not protected
by copyright and may be used for commercial and noncommercial purposes without fee or copyright
restriction" ([Smithsonian Terms of Use](https://www.si.edu/termsofuse)). Notes from the same terms:

- CC0 "pertains only to the copyright status of the Content." The Smithsonian "does not guarantee that all
  Content marked with the [CC0] icon is free from rights held by third parties," such as trademark, privacy
  or publicity rights. Some objects show trademarks or the names and likenesses of people (for example
  costumes, branded products and sports equipment); you are responsible for any such rights in your use.
- Attribution is not required, but the Smithsonian asks for a credit line of title, source, license and
  source URL; `record_link` gives the persistent link for each object.
- Do not use Smithsonian names or logos to imply endorsement. This mirror is not affiliated with or
  endorsed by the Smithsonian Institution.

The Smithsonian notes that collection language "reflects the context and culture of the time of its
creation, and may include culturally sensitive information." See the
[Open Access Values Statement](https://www.si.edu/openaccess).

## Provenance

Metadata comes from the weekly Open Access metadata dumps (`s3://smithsonian-open-access/metadata/edan/`);
files come from the public Open Access S3 bucket and the Smithsonian 3D API (`3d-api.si.edu`). Packages
listed in the 3D API without a CC0 Open Access record are excluded, as are CC0 packages without a
downloadable GLB; see [`meta/excluded.jsonl`](./meta/excluded.jsonl).

## Attribution

Credit: **Smithsonian Institution**, Smithsonian Open Access (https://www.si.edu/openaccess). Per-object
credit and links are in `data_source` and `record_link`.
