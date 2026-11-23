# D09 기존 외피 교체 범위 독립 감사

2026-10-13. 소스·저장된 장면/프리팹·원본 census·변경 전 충돌 스냅샷을 읽어 확인했다. 본 담당은 Unity를 실행하거나 장면을 변경하지 않았다.

## 핵심 결론

CLINIC·TOOLS·POWER·PARTS의 원본 물리 외벽은 `VisualRoot` 아래에 없다. V08 semantic anchor의 `VisualRoot`와 `CollisionRoot`는 형제이며, 현 D09 census가 랜드마크 시각 루트만 조사해 원본 외벽 충돌과 별도 지붕을 누락했다. **보이는 외피만 숨기고 이 충돌을 남기면 CLINIC의 새 중정/후퇴 외벽 등에 보이지 않는 옛 벽이 남는다.** 전체 충돌 루트를 비활성화하면 반대로 바닥·실내 설비 충돌도 사라진다.

원본 `BUILDING_SURVEY.json`을 덮어쓰지 않고, 새 `StageABuildingD09SourceAudit.WriteSupplementalSurvey(map)`를 D09 적용 전에 한 번 호출해 `SUPPLEMENTAL_SURVEY.json`을 추가한다. 쓰기는 `FileMode.CreateNew`이며 기존 추가 조사 파일을 덮어쓰지 않는다. 실제 추가 조사 실행·결과 검토는 메인이 한다.

## 정확한 범위

| 대상 | 교체할 원본 외피 | 보존할 부분 |
|---|---|---|
| 4개 V08 랜드마크 시각 | `02_Visual_Replaceable/<ID>__Visual/Generated/Override_<ID>/FactoryFrame/Exterior_FactoryShell`의 모든 외피 렌더러 | 같은 FactoryFrame의 `Interior_Role_<ID>/FactoryFloor`·실내 소품, `D04_Interiors_<ID>`, 논리 anchor·core |
| 4개 V08 랜드마크 벽 충돌 | `01_Collision_Replaceable_Separately/<ID>__Collision/Wall_XX_XX_Collision` | 같은 루트의 `Floor_Collision`, `V12_FactoryInteriorCollision` 전체 |
| 4개 V08 랜드마크 지붕 시각 | `structure.FindAnchor("ROOF_" + id).VisualRoot` 아래 원본 지붕 및 D04/D08 지붕 외피 | 지붕 semantic anchor와 원래 roof-zone 참조·게임플레이 정보 |
| 4개 V08 랜드마크 지붕 충돌 | `01_Collision_Replaceable_Separately/ROOF_<ID>__Collision/Roof_Collision` | 다른 semantic anchor의 충돌·기초 |
| V33 부속 64동 시각 | `EnclosuresV33_ConnectedFacilities/Buildings/<ID>/Visual/.../FactoryFrame/Exterior_FactoryShell`, 기존 `RoofCutaway`의 원본/D04/D08 외피 | `Interior_Role_*`, `FactoryFloor`, `Foundation` 전체, 문·실내·loot anchor |
| V33 부속 64동 충돌 | metadata `CollisionRoot`의 직접 외곽 벽/기둥/폐허 지붕 지지 shape 및 `V14_VisibleStructureCollision` | `V12_FactoryInteriorCollision`, `InteriorPartition_*`의 기존 상태, `Foundation/WalkableFloor`·Skirt·EntryRamp |
| HALL | D08의 별도 장식 `D08_Facade_HALL`, `D08_Roof_HALL`만 새 장식과 중복 시 별도 허용 목록으로 조정 가능 | 활성 `V13_HallRoot`의 실제 다층 구조·외벽·지붕·바닥·계단·실내·터널·충돌·가시성 구성 |

`FactoryFrame`, 랜드마크 `__Visual`, 부속동 `Visual`, 건물 holder, `CollisionRoot` 자체를 통째로 끄면 보존 대상도 함께 사라진다. 교체는 정확히 소유권이 확인된 외피 렌더러/외곽 collider의 상태만 바꾸고, 대응 D09 면·충돌이 준비된 경우에 적용해야 한다. 기존 비활성 요소를 켜는 과정은 필요 없다.

변경 전 `BEFORE_COLLISION_TRANSFORMS.txt`에서 확인한 물리 개수는 다음과 같다. 아래 수치는 `|C|` collider 행만 세었으며 transform 행을 이중 계산하지 않았다.

| 랜드마크 | 원본 외벽 | 별도 원본 지붕 | 보존할 V12 실내 충돌 |
|---|---:|---:|---:|
| CLINIC | 6 | 1 | 5 |
| TOOLS | 5 | 1 | 8 |
| POWER | 6 | 1 | 7 |
| PARTS | 6 | 1 | 8 |

부속 64동의 원본 census에는 `Collision/V14_VisibleStructureCollision/...` collider 312행, 보존할 `Collision/V12_FactoryInteriorCollision/...` 292행이 있다. 외곽 원본 이름은 다음 계열이다.

- V09: `North_Collision`, `South_Collision`, `East_Collision`, `West_Collision`, `North|South|West_A_Collision`, `_B_Collision`, `_Lintel_Collision`, `NorthPostA_Collision`, `NorthPostB_Collision`.
- V11: `Front_L__Collision`, `Front_R__Collision`, `Front_Lintel__Collision`, 대응 `Back_*`, `West__Collision`, `East__Collision`, `West_Broken__Collision`, `East_Broken__Collision`, `RuinRoofSupport_*__Collision`, `ShedPost_*__Collision`.
- `InteriorPartition_*`는 외곽 이름 목록에 넣지 않는다. V14가 기존 일부 partition을 이미 비활성화했으므로 **보존은 현재 활성/비활성 상태 유지**를 뜻하며 재활성화가 아니다. V12 실제 `RoomPartition_*`는 실내 충돌 루트에 있다.

이 이름들을 지도 전체에서 포괄적으로 검색해 끄면 안 된다. 먼저 `StageAEnclosuresV33Building.PlanId`와 `CollisionRoot` 또는 `StageABlockoutSemanticAnchor.SemanticId`로 정확한 건물 소유권을 확보한 후 한정한다. 같은 이름의 core 벽·시설 경계·다른 건물은 이 교체 범위가 아니다.

## 추가 조사 API와 보존 검증

`CollectExtraOriginalParts(map)`는 69행의 `SupplementalSurvey`를 반환한다. 각 행은 원본 source visual/collision/roof 참조와 root 활성 상태, 원본 census에 없던 renderer/collider만 포함한다. 4개 랜드마크의 누락된 충돌·별도 지붕과 HALL의 비활성 V08 원본이 추가되고, 부속동은 census가 holder 전체를 이미 조사해 대체로 추가 부품이 없다.

추가 `Part`는 기존 Census.Part의 `path/name/type/enabled/active/min/max/position/rotation/scale`를 상속하고 다음을 보완한다.

- `localBoundsCenter/Size`, `worldMin/Max`, `boundsReliable`: BoxCollider·MeshCollider의 **로컬 형상을 원본 building frame으로 직접 변환**한다. 회전된 월드 AABB를 다시 회전해 벽 두께를 과대평가하지 않는다.
- `activeSelf`, `isTrigger`, `instanceId`, `componentIndex`, `siblingIndexPath`: 활성 상태와 정확한 구성요소 식별을 보존한다. V14에는 실제 같은 path/name/type이 중복된 형제가 있으므로 경로만으로 단일 객체를 선택하면 누락될 수 있다. instanceId는 동일 Unity 세션에서만 유효하고, 재로드 비교는 siblingIndexPath+componentIndex를 함께 사용한다.
- `disposition`: `ReplaceExterior`, `ReplaceRoof`, `ReplaceEarlierArchitecture` 등 감사 권고이다. 자동 변경 기능이나 포괄적 변경 허가는 아니다.

추가 조사 전에 메인은 외부 시점의 기존 cutaway/preview 상태를 원본 촬영과 동일하게 맞춰야 한다. 적용 후에는 원본+추가 조사의 모든 보존 대상이 그대로인지 비교하고, 교체된 원본 상태는 `D09_STATE_ALLOWLIST.json`에 명시한다. 새 중정과 후퇴 벽의 통과 경로에서 숨은 원본 벽·지붕이 남지 않았는지 실제 충돌 샘플/이동으로 확인한다. 바닥·실내 설비·core·문/loot anchor도 별도로 확인한다.

## HALL 주의

HALL의 V08 `01_Collision_Replaceable_Separately/HALL__Collision`는 원본 스냅샷에서 비활성이다. 실제 활성 건물은 `V13_HallRoot`이며 census의 HALL은 이 전체 root를 이미 조사한다. HALL의 `Floor_B1`, `Floor_1F`, `Floor_2F`, 각 `Rooms/Structure/Stairs`, 실제 `Exterior/Facade`, `Roof`, 연결 터널은 이번 전면 외피 교체 대상으로 묶지 않는다. `StageAHallV13LayerVisibility`는 렌더 가시성만 제어하고 물리 구조를 끄지 않는 계약을 갖는다. 새로운 HALL 구성도 실제 공간을 덮는 새 단층 shell을 만들지 않고 부가 장식으로 통합해야 한다.

## 확인한 근거

- `Runtime/Stage/Blockout/StageABlockoutSemanticAnchor.cs:53,73,109,170`: 논리/시각/충돌 형제 계약, `_Collision` 생성.
- `Runtime/Stage/Blockout/StageABlockoutStructureV08.cs:604,617,631,688,748`: 원본 바닥·외벽·별도 지붕·anchor 참조.
- `Editor/StageAFactoryBuildingsV12.cs:367,377,382,390,737`: `Exterior_FactoryShell`와 `Interior_Role_*` 분리, 별도 roof, 실제 실내 partition.
- `Editor/StageAEnclosuresV33Placement.cs:454,473,481`: 64동의 Visual/Collision clone 및 metadata 소유권.
- `Editor/StageACollisionV14Repair.cs:16,99,139`: V14 외벽/모서리 collider 생성 및 legacy 상태 처리.
- `Runtime/Stage/Blockout/StageAHallV13LayerVisibility.cs:7,88`: HALL의 렌더 전용 층 가시성과 물리 보존.
- 저장된 `Scenes/StageA_Relay_Blockout_v08.unity:477`은 prefab GUID `4430aee8a3032cb499e097f2efc83427`를 참조한다. 해당 실제 `Resources/StageMaps/StageA_Relay_Blockout_v08.prefab`에서 `V13_HallRoot`(545793), `CLINIC__Collision`(833382), `ROOF_CLINIC__Collision`(1873615) 및 외피/V14 collision 이름을 직접 확인했다.

메인 실행 후 보완 조사 파일과 실제 검증 결과를 읽기 전에는 이 감사 문서만으로 통합 통과를 주장하지 않는다.

## 메인 실행 추가 조사 확인

이후 메인이 생성한 `SUPPLEMENTAL_SURVEY.json`을 재질/감사 담당이 직접 읽어 확인했다. 69행이며 추가 부품은 예상한 CLINIC·PARTS·POWER·TOOLS·HALL에만 있고 부속 64동은 0개다. 4개 랜드마크는 각각 지붕 렌더러 3행과 collider 13/16/15/15행이며, 활성 교체 대상은 외벽 23개+지붕 4개=27개다. 바닥·V12 실내는 별도 보존으로 분류됐다. HALL은 비활성 구형 V08 renderer 105개·collider 25개가 추가 보존 목록에 들어갔다. 모든 추가 부품의 `boundsReliable`가 true이다. 이는 **누락 조사 보완의 범위 검증**이며 D09 충돌·실제 이동·저장 재로드 검증의 통과를 대신하지 않는다.
