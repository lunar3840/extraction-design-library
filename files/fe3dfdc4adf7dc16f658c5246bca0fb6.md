# Free Pack - Gun 적용 결과

- C:/UnityProject/escape에 필요한 파일 39개를 GUID 유지하여 반영했다.
- PolyOne Studio 원본 5종 모델과 텍스처 → URP 재질/그립/총구/불꽃을 포함한 장착용 프리팹.
- SdGunLibrary, SdGunModel, SdWeaponPose 추가. SdCharacterVisual에서 자동 연결.
- 기본 소총 M16A1 / AR_004 AK47 / 권총 Five-Seven / 저격총 Barrett. 샷건은 번들에 없어 M16 임시 외형. MP5는 수동 배정 가능한 프리팹으로 준비.
- 양손 관절 자세, 조준, 발사 반동/불꽃/탄도 선 시작점, 간단한 재장전 손 위치, 미장착/근접 전환/구르기/사망 표시 처리.
- 총 모델은 시각용이며 충돌체/물리체가 없다. 기존 피해·탄약·AI·저장 규칙을 변경하지 않았다.

검사 결과: Gun 42 + Weapon 54 + Enemy 98 = 194 PASS. 세 Unity 배치 exit 0. 별도 URP 그래픽 렌더링 `Gun-Characters.png` 확인. 원본 반영 스크립트4개와 검사본의 내용 일치, 가져온 파일39개 해시 일치 확인.

제한: 세부 탄창 교체/노리쇠 조작/손가락 파지 애니메이션은 미포함. 구르기 중 총을 숨겼다가 복원. 31종 무기는 대표 외형을 공유한다.

상세 안내: C:/UnityProject/escape/Assets/Escape/SD_GUNS_README.md.
변경 전 연결 코드 백업: work/gun-backup/SdCharacterVisual.cs.
검사 자료: Gun-GunChecks.txt, Gun-WeaponChecks.txt, Gun-EnemyChecks.txt, Gun-ImportedFiles.json.
