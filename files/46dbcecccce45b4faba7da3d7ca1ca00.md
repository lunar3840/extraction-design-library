# 전체 UI 증빙 목록

2026-10-23 KST. 이 문서는 실제 파일 목록·PNG 헤더·기존 검증 기록을 읽어 정리한 색인이다. 별도 Unity 실행·입력 검사를 수행했다는 뜻은 아니다. **로비 결과 배너의 마지막 보완은 아래 실제 재검증 항목으로 분리한다.**

## 기준과 검증 범위

- 주 비교: 실제 Game View **1920×1080 변경 전 37장 ↔ after_r02 37장**. 같은 시드 20261023·상태 경로를 사용했으나 실시간 초·카메라 보간·월드 프레임까지 픽셀 동일하지는 않다.
- 추가 해상도: 실제 **1600×900 변경 후 37장**. 1600×900 before는 없으므로 작은 해상도의 전후 비교로 부르지 않는다.
- 이 색인 작성 시 위 111장과 native 7장, **총 118장**의 실재·PNG 헤더 해상도·같은 이름의 `.state.txt` 존재를 확인했다. 정지 이미지/geometry 확인은 native 동작 성공을 대신하지 않는다.
- 세 경로 manifest 모두 captures=37, geometryFailures=0, titleNoSession=True 및 COMPLETE를 기록한다. 삭제된 의료 휠·숫자 소모품 퀵슬롯은 검사/구현 대상에 복구하지 않았다.

원본 실행 기록: [before1920](before_manifest_1920x1080.txt) · [after_r02 1920](after_r02_manifest_1920x1080.txt) · [after1600](after_manifest_1600x900.txt). 화면별 상태 로그는 각 PNG와 동일한 기본 이름의 `.state.txt`다.

## 37개 상태 대응표

아래 **후_r02**가 1920×1080 통합 비교의 대표 세트다. **추가1600**은 해상도 검수이며, 로비의 결과 배너 겹침을 남긴 이전 상태도 포함한다. 그 배너의 최종 보완 결과는 이 표의 이미지로 통과 처리하지 않는다.

| # | 실제 상태 | 전 1920×1080 | 후_r02 1920×1080 | 추가 1600×900 |
| --- | --- | --- | --- | --- |
| 1 | 메인 | [전](before_title_1920x1080.png) | [후_r02](after_r02_title_1920x1080.png) | [추가](after_title_1600x900.png) |
| 2 | 새 게임 슬롯 | [전](before_new_slots_1920x1080.png) | [후_r02](after_r02_new_slots_1920x1080.png) | [추가](after_new_slots_1600x900.png) |
| 3 | 불러오기 · 빈 슬롯 | [전](before_load_slots_empty_1920x1080.png) | [후_r02](after_r02_load_slots_empty_1920x1080.png) | [추가](after_load_slots_empty_1600x900.png) |
| 4 | 새 게임 확인 | [전](before_new_slot_confirmation_1920x1080.png) | [후_r02](after_r02_new_slot_confirmation_1920x1080.png) | [추가](after_new_slot_confirmation_1600x900.png) |
| 5 | 기지 입장 로딩 | [전](before_loading_1920x1080.png) | [후_r02](after_r02_loading_1920x1080.png) | [추가](after_loading_1600x900.png) |
| 6 | 기지 이동 HUD | [전](before_camp_hud_1920x1080.png) | [후_r02](after_r02_camp_hud_1920x1080.png) | [추가](after_camp_hud_1600x900.png) |
| 7 | 출격 로비 | [전](before_lobby_1920x1080.png) | [후_r02](after_r02_lobby_1920x1080.png) | [추가](after_lobby_1600x900.png) |
| 8 | 인벤토리 정면 | [전](before_inventory_1920x1080.png) | [후_r02](after_r02_inventory_1920x1080.png) | [추가](after_inventory_1600x900.png) |
| 9 | 인벤토리 측면 | [전](before_inventory_side_1920x1080.png) | [후_r02](after_r02_inventory_side_1920x1080.png) | [추가](after_inventory_side_1600x900.png) |
| 10 | 인벤토리 의료 상세 | [전](before_inventory_medical_1920x1080.png) | [후_r02](after_r02_inventory_medical_1920x1080.png) | [추가](after_inventory_medical_1600x900.png) |
| 11 | 무기 개조 | [전](before_weapon_modding_1920x1080.png) | [후_r02](after_r02_weapon_modding_1920x1080.png) | [추가](after_weapon_modding_1600x900.png) |
| 12 | 보관함 | [전](before_storage_1920x1080.png) | [후_r02](after_r02_storage_1920x1080.png) | [추가](after_storage_1600x900.png) |
| 13 | 보관함 상세 | [전](before_storage_details_1920x1080.png) | [후_r02](after_r02_storage_details_1920x1080.png) | [추가](after_storage_details_1600x900.png) |
| 14 | 상점 | [전](before_supply_1920x1080.png) | [후_r02](after_r02_supply_1920x1080.png) | [추가](after_supply_1600x900.png) |
| 15 | 공용 연구 | [전](before_research_1920x1080.png) | [후_r02](after_r02_research_1920x1080.png) | [추가](after_research_1600x900.png) |
| 16 | 동료 능력 | [전](before_companion_abilities_1920x1080.png) | [후_r02](after_r02_companion_abilities_1920x1080.png) | [추가](after_companion_abilities_1600x900.png) |
| 17 | 회수 칩 | [전](before_chip_recovery_1920x1080.png) | [후_r02](after_r02_chip_recovery_1920x1080.png) | [추가](after_chip_recovery_1600x900.png) |
| 18 | 의뢰 | [전](before_quests_1920x1080.png) | [후_r02](after_r02_quests_1920x1080.png) | [추가](after_quests_1600x900.png) |
| 19 | 저장 관리 | [전](before_save_records_1920x1080.png) | [후_r02](after_r02_save_records_1920x1080.png) | [추가](after_save_records_1600x900.png) |
| 20 | 출격 준비 | [전](before_departure_1920x1080.png) | [후_r02](after_r02_departure_1920x1080.png) | [추가](after_departure_1600x900.png) |
| 21 | 출격 전환 | [전](before_departing_1920x1080.png) | [후_r02](after_r02_departing_1920x1080.png) | [추가](after_departing_1600x900.png) |
| 22 | 플레이 HUD | [전](before_combat_hud_1920x1080.png) | [후_r02](after_r02_combat_hud_1920x1080.png) | [추가](after_combat_hud_1600x900.png) |
| 23 | 리그 의료 추천 | [전](before_medical_recommendation_1920x1080.png) | [후_r02](after_r02_medical_recommendation_1920x1080.png) | [추가](after_medical_recommendation_1600x900.png) |
| 24 | 치료 진행 | [전](before_medical_treatment_1920x1080.png) | [후_r02](after_r02_medical_treatment_1920x1080.png) | [추가](after_medical_treatment_1600x900.png) |
| 25 | 상태이상 | [전](before_medical_status_1920x1080.png) | [후_r02](after_r02_medical_status_1920x1080.png) | [추가](after_medical_status_1600x900.png) |
| 26 | 주변 파밍 목록 | [전](before_loot_prompt_group_1920x1080.png) | [후_r02](after_r02_loot_prompt_group_1920x1080.png) | [추가](after_loot_prompt_group_1600x900.png) |
| 27 | 전리품 수색 중 | [전](before_loot_search_1920x1080.png) | [후_r02](after_r02_loot_search_1920x1080.png) | [추가](after_loot_search_1600x900.png) |
| 28 | 전리품 수색 완료 | [전](before_loot_revealed_1920x1080.png) | [후_r02](after_r02_loot_revealed_1920x1080.png) | [추가](after_loot_revealed_1600x900.png) |
| 29 | 전술 지도 | [전](before_tactical_map_1920x1080.png) | [후_r02](after_r02_tactical_map_1920x1080.png) | [추가](after_tactical_map_1600x900.png) |
| 30 | 탈출 진행 | [전](before_extraction_1920x1080.png) | [후_r02](after_r02_extraction_1920x1080.png) | [추가](after_extraction_1600x900.png) |
| 31 | 탈출 후 기지 | [전](before_extracted_camp_1920x1080.png) | [후_r02](after_r02_extracted_camp_1920x1080.png) | [추가](after_extracted_camp_1600x900.png) |
| 32 | 대화 | [전](before_story_dialogue_1920x1080.png) | [후_r02](after_r02_story_dialogue_1920x1080.png) | [추가](after_story_dialogue_1600x900.png) |
| 33 | 대화 기록 | [전](before_story_history_1920x1080.png) | [후_r02](after_r02_story_history_1920x1080.png) | [추가](after_story_history_1600x900.png) |
| 34 | 대화 건너뛰기 확인 | [전](before_story_skip_confirmation_1920x1080.png) | [후_r02](after_r02_story_skip_confirmation_1920x1080.png) | [추가](after_story_skip_confirmation_1600x900.png) |
| 35 | 사망 · 작전 실패 | [전](before_death_1920x1080.png) | [후_r02](after_r02_death_1920x1080.png) | [추가](after_death_1600x900.png) |
| 36 | 저장 전환 | [전](before_saving_1920x1080.png) | [후_r02](after_r02_saving_1920x1080.png) | [추가](after_saving_1600x900.png) |
| 37 | 메인 복귀 | [전](before_title_return_1920x1080.png) | [후_r02](after_r02_title_return_1920x1080.png) | [추가](after_title_return_1600x900.png) |

## 실제 화면 검수와 native 입력

- 독립 시각 검수: [HUD·파밍](staging/hud/ACTUAL_SCREEN_REVIEW.md), [공통 화면](staging/common/ACTUAL_SCREEN_REVIEW.md). 초기 발견된 파밍 목록 중복 월드 표식과 작은 버튼 이음선은 r02 재촬영 후 해소 확인 기록이 있다. 두 문서의 첫 판정과 후속 판정을 구별한다.
- 두 문서는 1600×900 추가 원본도 직접 열어 확인했다. 각 검수자가 명시한 화면 범위만 인정하며, geometryFailures=0만으로 모든 글자의 읽힘을 보증하지 않는다.
- 실제 마우스 관측과 한계: [NATIVE_INPUT_REVIEW](NATIVE_INPUT_REVIEW.md), [native 실행 manifest](interactive_manifest_1920x1080.txt). 7개 캡처는 동작별 분류이며 정확한 무입력 순간을 보증하지 않는다. 로비↔기지 버튼 전환은 창 관측 기록이 있으나 별도 로비 PNG는 없다.

| native 캡처 분류 | 실제 Game View 1920×1080 | 상태 로그 |
| --- | --- | --- |
| 메인 캐릭터 드래그 | [화면](interactive_native_title_1920x1080.png) | [상태](interactive_native_title_1920x1080.state.txt) |
| 새 게임·슬롯 선택 | [화면](interactive_native_slots_1920x1080.png) | [상태](interactive_native_slots_1920x1080.state.txt) |
| 기지 진입 | [화면](interactive_native_camp_1920x1080.png) | [상태](interactive_native_camp_1920x1080.state.txt) |
| 출격 장비 탭 | [화면](interactive_native_inventory_1920x1080.png) | [상태](interactive_native_inventory_1920x1080.state.txt) |
| 헬멧 더블클릭 장착 | [화면](interactive_native_equipment_1920x1080.png) | [상태](interactive_native_equipment_1920x1080.state.txt) |
| 탄약 드래그 이동 | [화면](interactive_native_drag_1920x1080.png) | [상태](interactive_native_drag_1920x1080.state.txt) |
| Y912 체력 영역 클릭 | [화면](interactive_native_footer_1920x1080.png) | [상태](interactive_native_footer_1920x1080.state.txt) |

**미검증 키보드·거래 범위:** 자동 조작의 Tab/I 입력으로 게임 내 전환이 관측되지 않아 Tab/I/F/V/R 등의 native 단축키 성공을 주장하지 않는다. 물리 키보드 기능 실패로 확정한 것도 아니다. 열린 파밍창 전송·장비 해제·드롭·지역 맞교환을 이 native 세션에서 전부 반복하지 않았고, hover/focus/모든 스크롤 끝·오류 복구도 전수 검증하지 않았다. 1280×720·1366×768·비16:9·다양한 OS 배율은 미검증이다.

## 순수 모델 회귀와 적용 증빙

[적용 후 모델 결과](staging/common/offline/post_apply_01/results.txt) · [검사 범위·소스 대조](staging/common/offline/post_apply_01/POST_APPLY_REVIEW.md) · [독립 컴파일](staging/common/offline/post_apply_01/compile.txt)

- 실제 적용 DLL 기준 89 assertions: 더블클릭 sequence24 + 리그/격자/전리품17 + 유한 이동/지역 교환10 + 자동장전38. 별도로 seeded 10,000회 이동 각각에서 수량 보존을 확인했다. compiler/runner exit0.
- 이 검사는 in-memory 모델/규칙 검사다. Unity GameObject·실제 UI·native 입력·장면·사용자 저장 동작을 검증한 것으로 확대하지 않는다. 검사 당시 DLL과 직접 규칙 소스의 전후 해시는 해당 폴더에 보존돼 있다.
- 마지막 배너 및 다른 담당 타이틀 변경을 포함한 최종 DLL에도 [새 모델 회귀](staging/common/offline/post_apply_final/FINAL_MODEL_REVIEW.md)를 실행했다. DLL SHA256 `F95A61D863E7FA00030E829E12DA68BE32F2AD474BB5D3E56B71C7F30CB3B63C`, fresh compile/runner exit0,89 assertions+10,000회 수량 보존 통과, 입력8개 해시 전후 동일이다. 이전 v3 전체 일치를 주장하지 않으며 실제 UI/입력 증거와 구별한다.
- 원본 코드: [변경 전 SHA256](production_before/SOURCE_HASHES.json). 배경 에셋 실제 import: [after_asset_import](after_asset_import.txt). 전체 UI를 한 장의 이미지로 덮은 구현이 아니다.

## Figma 21화면 대응

[Figma revision 페이지](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0?node-id=178-559) · [상세 Figma→Unity 매핑](FIGMA_UNITY_MAPPING.md)

| Figma 화면 번호·이름 | 이 증빙의 실제 표시 경로 |
| --- | --- |
| 01 파밍·리그·배낭 / 08 보관함 / 21 아이템 상세 | 인벤토리 정면·측면·의료 상세, 전리품 수색/완료, 보관함/상세 |
| 02 플레이 HUD | 플레이 HUD, 리그 의료 추천·치료 진행·상태이상 |
| 03 메인 | 메인, 메인 복귀 및 native 캐릭터 드래그 |
| 04 기지 출격 로비 / 05 출격 준비 / 14 기지 이동 HUD | 출격 로비, 출격 준비/전환, 기지 HUD |
| 06 저장 슬롯 / 07 확인·로딩·오류 | 새 게임·빈 불러오기·확인·로딩·저장 전환·저장 관리. 오류 복구의 native 성공은 미검증 |
| 09 상점 | 상점 |
| 10 공용 연구 / 11 동료 능력 / 12 회수 칩 | 공용 연구, 동료 능력, 회수 칩 |
| 13 의뢰·기록 | 의뢰 |
| 15 전술 지도 / 16 무기 개조·탄약 | 전술 지도, 무기 개조 |
| 17 동료 칩 | 기존 CompanionChipOverlay/선택 UI 공통 스타일 경로. 이번 37상태의 독립 전후 캡처는 없음 |
| 18 대화·기록 | 대화, 대화 기록, 건너뛰기 확인 |
| 19 탈출·귀환 / 20 사망·시간 만료 | 탈출 진행·기지 귀환, 사망·작전 실패. 시간 만료를 별도 native 재현한 증빙은 아님 |

Figma 21개는 디자인 화면 수이고 실제 캡처 상태 37개·native 캡처 7개와 일대일 개수 대응하지 않는다. Figma 예시 인물·총기·아이템·수치는 샘플이며 Unity는 현재 모델/상태를 사용한다. 없는 제작·정지·설정 기능을 연결한 것으로 해석하지 않는다.

## 게시 대표에서 제외한 이력

- `before_editor_handoff.png`: 다른 앱에 가린 잘못된 캡처이며 이미 제거된 파일. 변경 전 증빙이나 게시 대표로 사용하지 않는다.
- `after_figma_lobby_result.png`: 오래된 Figma 결과 배너 시안. 실제 Unity 최종 검증으로 사용하지 않는다.
- `after_figma_overview.png` 등 옛 overview/초기 after·Figma 캡처는 작업 이력으로만 보존하고 이 색인의 대표 전후 세트를 대체하지 않는다. Figma 시안과 실제 Game View를 혼용하지 않는다.

## 로비 결과 배너 최종 보완 · 실제 재검증 완료

**해소 확인.** 기존 1600×900 로비 원본의 ResultSummary가 이름/동료 칩 선택 줄을 가렸다. 결과 안내만 우측 출격 패널의 버튼 위로 옮겼다. 정상 사망→긴급 회수→로비 진입 경로로 원래 실패 문구 두 줄을 재현했으며, 임의의 private 결과 문자열을 주입하지 않았다.

- [보완 전 1600](after_lobby_1600x900.png) ↔ [보완 후 1600](after_r02_lobby_result_1600x900.png). 같은 해상도·같은 로비·동일 실패 안내 문구다. 전자는 이전 실패 결과가 남은 새 세션의 무기 장착 상태이고, 후자는 사망 후 무기를 잃은 상태이므로 장비/자세까지 동일한 비교는 아니다. 안내의 고정 표시 영역과 이름 영역의 충돌/분리를 비교한다.
- [최종 1920](after_r03_lobby_result_1920x1080.png). 기존37상태에 lobby_result를 더한 두 route 모두43단계·38캡처·geometryFailures0·Title/noSession cleanup을 완료했다: [1920 manifest](after_r03_manifest_1920x1080.txt), [1600 manifest](after_r02_manifest_1600x900.txt).
- 메인과 독립 검수자가 두 원본을 직접 확인했다. 결과 문구 두 줄이 전부 보이고 이름/칩 선택 버튼과 분리되며, 아래 출격 버튼과 약26px/21px 간격이 남는다. 최신 판정은 [공통 화면 검수](staging/common/ACTUAL_SCREEN_REVIEW.md) 말미에 있다.
- [최종 Figma 로비](after_figma_lobby_result_v2.png)는 편집 가능한 조건부 notice와 기존 semantic token으로 같은 배치를 정리한 디자인 증빙이다. 실제 Unity 이미지와 구별한다. 첫 stale render와 옛 overview를 최종으로 사용하지 않는다.
- 마지막 재검증에는 다른 담당의 최신 catalog 기반 타이틀 모델 전환이 병합된 production이 사용됐다. [최신 타이틀 참고](after_r03_title_1920x1080.png). 그 캐릭터 기능의 설계/검증 회차는 별도 담당 기록에 속하며 이 UI 지시의 횟수로 합산하지 않는다.

라이브러리 게시 완료 여부는 이 색인의 파일 저장만으로 판정하지 않는다. 주제/회차와 실제 게시 상태는 [변경 기록](CHANGE_RECORD.md)의 최종 갱신을 따른다.

