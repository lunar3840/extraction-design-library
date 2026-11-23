# 인게임 과노출 원인 조사 — 2026-10-21

사용자 요청: 갑자기 인게임 색조와 밝기가 너무 밝아진 원인을 확인한다. 이번 작업은 원인 조사이며 제품의 그래픽 설정이나 썸네일 코드를 수정하는 작업은 아니다.

## 확인한 직접 원인

Unity Editor 메모리에 이전 실행의 무기 썸네일용 숨김 방향광이 남아 누적되어 있다. 씬이 유효하지 않은 비영구 `HideAndDontSave` 오브젝트도 실제 게임 카메라의 네이티브 광원 목록에 포함되며 추가 조명으로 맵을 밝힌다.

`WeaponModelThumbnails.cs`는 `Weapon thumbnails — render only` 루트와 별도 `Weapon thumbnail studio` 씬에 `Thumbnail key`(2.1), `Thumbnail fill`(1.2)을 생성한다. 레이어 29 cullingMask를 지정했지만 현재 PC Forward+ 경로에서 게임 화면과의 광원 격리를 보장하지 못한다. 루트 정적 참조와 캐시 정리 외에 별도 씬 언로드가 없다는 수명 관리 결함이 의심된다. 어느 Unity 이벤트에서 최초 누수가 발생하는지는 이번 조사에서 확정하지 않았다.

## 원인 분리 실험

- 기존 에디터는 A_Relay 씬, Edit, dirty=false였다. 현재 편집 화면에서도 과노출을 확인했다.
- 원래 사용자 저장과 분리된 StageScenePreview의 임시 GUID 슬롯에서 실제 A맵 카메라를 촬영했다.
- 첫 전체 썸네일 조명 비활성화 실험은 밝기 차이가 컸으나 숨김 오브젝트를 모두 포함하므로 후속 실험으로 범위를 좁혔다.
- `repeat-isolation/`: 현재 유효한 스튜디오의 두 광원만 끈 경우 거의 변화가 없다. 과거 조명이 계속 남아 있다.
- `orphan-isolation/`: 현재 카메라가 보는 광원은 태양 1개와 썸네일 방향광 38개였다. 유효하지 않은 씬의 고아 방향광 36개만 비활성화하면 태양 1개와 현 스튜디오 2개만 남고 과노출이 사라졌다. 다시 36개를 복원하면 최초 과노출 PNG와 SHA256까지 완전히 같았다.
- 비교 중 태양 `Test Sun` 강도 1.5, PC 파이프라인, Linear 색공간, Neutral 톤매핑, 노출 0은 유지했다. 주광이 바뀐 것이 아니라 추가 방향광 누적 경로가 관측 결과와 부합한다.
- 독립 검토 에이전트도 세 PNG와 광원 메타데이터·네이티브 컬링 로그를 직접 확인했다. 고아 조명만 끈 전후의 4px 격자 표본 평균 채널 차는 48.84/255, 복원본은 원본 PNG와 동일하다.

## 사진과 로그

- `orphan-isolation/current_game_camera.png`: 고아 조명이 켜진 현재 과노출 상태.
- `orphan-isolation/diagnostic_thumbnail_lights_off.png`: 고아 조명 36개만 일시적으로 끈 진단 비교 화면. **제품 수정 완료 화면이 아니다.**
- `orphan-isolation/diagnostic_thumbnail_lights_restored.png`: 조명을 복원한 화면. 최초 PNG와 동일하다.
- `orphan-isolation/orphan_lights.txt`: 인스턴스 ID, 비영구 여부, HideAndDontSave, 부모 루트, sceneValid=false.
- `orphan-isolation/native_cull_current.txt`, `native_cull_thumbnail_off.txt`, `native_cull_restored.txt`: 실제 게임 카메라의 광원 목록.
- 초기 `snapshot_*.txt`: 현재 편집/플레이 조명·색공간·후처리 상태.

사진은 1600×1000 실제 게임 카메라 렌더이며 HUD Overlay는 포함하지 않는다. 같은 Play의 동일 카메라로 연속 비교했고, 복원 사진의 파일 해시 일치로 외부 장면 변화가 원인이 아님을 확인했다. 다른 Play의 이미지끼리는 위치·날씨가 다를 수 있으므로 직접 짝지어 비교하지 않는다.

## 제외한 설명

- V39 기준과 현재의 맵 씬, 조명·색 보정 코드, 지역 Volume, 색공간·GraphicsSettings는 동일하다. 카메라 각도 변경이 이 증상의 직접 원인이라는 근거는 없다.
- QualitySettings의 Mobile 항목 삭제와 PC 인덱스 1→0은 PC 선택의 재번호다. 전후 PC 파이프라인은 같다. PC 실시간 반사 프로브 0→1은 기존 런타임에서도 활성화하던 값이며 10/20의 최초 증상보다 나중에 바뀌었다.
- 지역 노출 +0.25는 기존 설정이고, 과노출이 재현된 외곽 표본에서는 실제 적용 노출이 0이었다.

## 정리와 현재 상태

모든 조명 토글은 finally에서 원상복구했다. 진단용 Play 3회가 같은 결함으로 새로 만든 스튜디오 3개(조명 6개)는 인스턴스 ID와 루트를 검증해 제거했다. 조사 시작 전부터 있던 고아 조명 32개는 남아 있으며 근본 수정은 아직 적용하지 않았다. `orphan-isolation/diagnostic_cleanup.txt`에 38→32 및 Play=false/dirty=false를 기록했다. 따라서 현재 세션의 원래 과노출 문제가 해결됐다고 보고하지 않는다.

임시 에디터 진단 코드와 메타는 원본 프로젝트에서 제거했고 소스는 `DIAGNOSTIC_PROBE.cs.txt`에 보존했다. 원본 씬·프리팹·그래픽 설정은 저장하지 않았다. 기존 임포트 요청 도구가 작성한 과거 COMMAND_refresh 로그는 호출 전 복사본으로 복원한다.

## 수정 방향

썸네일 조명/카메라/스튜디오 씬의 생성·종료 수명을 명시적으로 관리하고, 게임 카메라가 썸네일 조명을 누적하지 않도록 렌더링을 격리해야 한다. 수정 후 반복 Play 시작·종료, 인벤토리 열기·닫기, 썸네일 갱신에서 광원 수와 맵 밝기가 유지되는지 확인한다. 이번에는 해당 제품 수정까지 진행하지 않았다.
