# 루팅·상점·툴팁 티어 색 정합 · Figma

완료. 기존 6색 Tier/Item을 재사용하고 공용 hover 정보창을 같은 색 변수에 연결한 6개 시각 변형으로 정리했다. 하단 ‘클릭 상세 · 드래그 이동’ 문구도 모두 삭제했다.

## 사용자 지시와 범위

루팅·상점 슬롯과 hover 정보창에 기존 가방의 6색 파스텔 배경·외곽선 기준을 통일한다. V/T·잔량·판매가·설명은 유지한다. 미확인 루팅의 색 노출이나 미구매 상품 회색 처리 같은 새 규칙은 추가하지 않는다. 클릭·드래그 기능, 프로토타입 연결 및 Unity 코드는 이 Figma 편집에서 변경하지 않았다.

파일 KUD4t84BL1S77c3cnb8co0. UI Kit 페이지0:1, 검토 페이지105:2985, Basecamp 페이지11:712. 공식 MCP get_libraries를 재점검했으나 Professional View 좌석 호출 한도 오류가 반환됐고 whoami로 좌석 상태를 확인했다. 이를 라이브러리 부재로 해석하지 않고 네이티브 Figma 편집으로 진행했다. Full seat 전환 창은 Close로 닫았으며 결제·좌석 변경, 앱 종료·재시작은 하지 않았다.

## 변경 전 상태와 재사용

- Tier/Item 11:429의 6등급 × Default/Selected 12변형은 큰 아이콘·우하단 plain 수량·tier tint24%를 이미 사용한다. 이 마스터와 기존 색 변수는 이번에 변경하지 않았다.
- 상점14:884의 네 상품 아이콘은 이미 Tier/Item 1~4를 사용한다. 실제 before 화면에서4색 슬롯을 확인했으므로 상품행·슬롯을 새로 만들거나 배치하지 않았다. 정적 시안의 상품을6개로 늘리지 않았다.
- 검토 인벤토리+루팅105:2987도 Tier/Item을 재사용한다. 캐릭터·열·슬롯 간격·커서 위치는 그대로다.
- tooltip 원본105:2984는304×260, dense 중립 배경과 초록 상단3px선만 있어 슬롯색 면·외곽선과 일치하지 않았다. 마지막 본문 줄에 조작 안내가 있었다.

## 실제 수정 노드

원본105:2984를 보존한 채 Inventory/ItemHoverTooltip 패밀리112:3402를 생성했다. Color tier=1~6 색 축이며 기존 원본은2번이다. 새 하위 컴포넌트는5개다.

| Color tier | 노드 | 기존 결합 변수 | 원색 |
|---|---|---|---|
| 1 | 112:3403 | color/tier/1 + color/tier/1/tint | #DCE2EC |
| 2 · 기존 원본 | 105:2984 | color/tier/2 + color/tier/2/tint | #ADE4BE |
| 3 | 112:3422 | color/tier/3 + color/tier/3/tint | #A9CEF5 |
| 4 | 112:3441 | color/tier/4 + color/tier/4/tint | #CCB8EC |
| 5 | 112:3460 | color/tier/5 + color/tier/5/tint | #EDD29A |
| 6 | 112:3479 | color/tier/6 + color/tier/6/tint | #EEB3B7 |

모두 기존 color/bg/dense (#283340,78.82%) 위에 해당 tier tint24%를 결합했다. 상단3px를 유지하고 좌·우·하단1px를 추가해 외곽선에도 같은 tier 변수를 결합했다. 모서리21, 패딩16, 세로gap8, Noto Sans KR과 기존 의료품 아이콘 인스턴스를 유지했다. 새 토큰을 만들거나 기존 토큰 값을 바꾸지 않았다.

본문 조작 안내 줄만 삭제해 본문 높이84→63, 카드 높이260→239로 줄였다. 최종304×239 고정 외형이며 본문 Text는 자동 높이를 유지한다. 원본을 임시 fixed260으로 정리한 뒤 최종239로 줄였다는 크기 모드 변경도 포함한다. 패밀리는2555,2740 / 992×538이고6카드 원본 보관 위치만3×2로 정리했다(x20/344/668,y20/279). 기존 화면 인스턴스의 좌표에는 영향을 주지 않는다.

**Color tier는 색상 검토용 시각 상태다.** 여섯 상태의 본문은 동일 의료품 예시(야전 응급키트 / V2 · 소모품 · 1개 / 남은 용량120/120 / 0.8kg · 판매가300블랭크 / 회복 키트 설명)다. 색1·3~6 선택이 텍스트·아이템 데이터를 자동으로 바꾼다는 뜻이 아니다. 실제 아이템별 V/T·이름·잔량과 hover 데이터 연결은 런타임 구현과 구별한다. T1~T6 의료품 카탈로그를 추가한 것이 아니다.

## 검증 결과

- 실제 네이티브 UI에서6상태를 각각 선택해 tint24%·동일 tier stroke·dense 결합,304×239,r21을 확인했다. figma-six-tier-native-audit.json 전 항목 true이며 after_tooltip_tierN_ax.txt에 개별 실제 AX를 보존했다.
- 기존 검토 인스턴스105:3363에 원본 변경이 자동 전파되었다. Color tier=2,304×239, 초록 면·외곽선과 V2/120/120 본문을 확인했다. 좌표906,530 유지. 안내 줄이 없으며 커서와 새로운 겹침·잘림은 보이지 않았다. after_review_tooltip_instance_ax.txt 보존.
- 6색 패밀리1× PNG와 검토 화면1×1920×1080 PNG를 직접 다시 열어 확인했다. 제목·잔량·설명은 읽히고6색이 구별된다. 기존 캐릭터 영역과 배치를 보존했다. 메인의 독립 검토도 두 PNG PASS이며 FIGMA_FINAL_VISUAL_REVIEW.md에 별도로 기록됐다.
- Figma prototype hover wiring은 새로 구성하지 않았다. 결과는 편집 가능한 컴포넌트·시각 색상 상태 및 기존 정적 검토 인스턴스의 갱신이다.

## 전후 증빙

| 파일 | 의미와 비교 조건 |
|---|---|
| before_loot_review_native.jpg/.png ↔ after_loot_review_native.jpg/.png | 동일1085×1025 창·검토 화면·24% 비교. tooltip 색/높이/안내 삭제와 배치 보존 |
| before_shop_native.jpg/.png | 상점4색 재사용 실제 확인. 의도적인 무수정으로 별도 after를 위장 생성하지 않음 |
| before_tooltip_master_native.jpg/.png | 중립 배경·260높이·안내가 있는 수정 전 원본133% |
| after_tooltip_family_native.jpg/.png | 완성6색 패밀리47% 전체. 단독 source before와 다른 대상/배율임 |
| after_review_tooltip_instance_native.jpg/.png | 기존 인스턴스 자동 전파133% 상세. source before와 문맥이 달라 같은 시점 쌍으로 주장하지 않음 |
| after_tooltip_tier2_intermediate_native.jpg/.png | tint2만 결합한 보존 중간본. 사방선·안내 삭제 전이며 최종 아님 |
| after_figma_tooltip_six_tiers.png | 실제 Figma1×992×538 PNG, 후처리 없음 |
| after_figma_loot_review.png | 실제 Figma1×1920×1080 PNG, 후처리 없음 |

네이티브 JPEG의 PNG 대응본은 포맷만 변환했다. 리사이즈·크롭·이미지 편집은 하지 않았다. 최종 크기·SHA256은 figma-final-image-manifest.json에 저장했다.

- 6색 원본 PNG SHA256: 41d1e2d4a4d0d8d55790ab75100d240f9037bbf9abfb05f918bb2d749438cd8c
- 검토 화면 원본 PNG SHA256: c382b911ab66967d73fab89bacad481f91c97003426d67828c8a0daaf3278838

## 정확한 Figma 링크

- [6색 tooltip 패밀리](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0/EXTRACTION-Modern-Glass-UI-Kit?node-id=112-3402)
- [기존 원본 · Color tier2](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0/EXTRACTION-Modern-Glass-UI-Kit?node-id=105-2984)
- [인벤토리+루팅 검토 화면](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0/EXTRACTION-Modern-Glass-UI-Kit?node-id=105-2987)
- [갱신된 기존 tooltip 인스턴스](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0/EXTRACTION-Modern-Glass-UI-Kit?node-id=105-3363)
- [기존 상점 화면](https://www.figma.com/design/KUD4t84BL1S77c3cnb8co0/EXTRACTION-Modern-Glass-UI-Kit?node-id=14-884)

마스터 상태 포인터와 Unity 파일은 이 에이전트가 변경하지 않았다. 마지막 네이티브 입력 이후에는 기록·해시만 마무리했다.