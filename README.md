# Lemon

Unity 6000.3.22f1 기반의 1인칭 공포 탐색 프로젝트입니다.

## 실행

Unity Hub에서 프로젝트를 열고 다음 씬을 실행합니다.

`Assets/_3DStealthGame/Tutorial_Demo/Demo_Scenes/DemoScene.unity`

## 조작

- WASD: 이동
- 마우스: 시점 회전
- E: 레몬 투시 스킬 켜기/끄기
- F: 문 열기/닫기 (잠긴 문은 해당 열쇠 필요)
- Escape: 마우스 잠금 해제

계속 이동하면 오른쪽 소음 게이지가 증가하고, 멈추면 감소합니다. 레드문 양쪽의 발소리 귀신은 각 구역에서 순찰하며 소리가 난 위치를 조사합니다.

## 프로젝트 구성

- `Assets`, `Packages`, `ProjectSettings`: Unity 프로젝트
- `ArtSource`: 플레이어 손 동작 제작용 Blender 원본
- `Assets/NoiseGhost`: 발소리 귀신 FBX, 이동 데이터, 소음 감지와 게이지

Unity 캐시, 로그, 개인 에디터 설정, 녹화 및 임시 검증 파일은 Git에서 제외합니다. 포함된 외부 에셋에는 각 원본의 라이선스가 적용됩니다.
