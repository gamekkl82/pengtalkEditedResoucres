using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Threading.Tasks;

public class CostumePopupController : PopupController
{
    [SerializeField] private Toggle mainToggleGroup_Default;
    [SerializeField] private Toggle costumeToggleGroup_Default;
    [SerializeField] private GameObject costumeGroup;
    [SerializeField] private BigGridScroll costumeScroll;
    [SerializeField] private GameObject bgGroup;
    [SerializeField] private BigGridScroll bgScroll;
    [SerializeField] private GameObject closetGroup;
    [SerializeField] private Toggle closetToggleGroup_Default;
    [SerializeField] private BigGridScroll closetScroll;
    [SerializeField] private GameObject albumGroup;
    [SerializeField] private BigGridScroll albumScroll;

    [SerializeField] private GameObject BG_Root;
    [SerializeField] private GameObject Buy_Button;
    [SerializeField] private Text Buy_Price;

    [SerializeField] private PengsooUIController lobbyUI;
    [SerializeField] private GameObject SmapleBtn;
    [SerializeField] private GameObject ResetBtn;

    [Header(" - CostumeGroup")]
    [SerializeField] private Color costumeFilter_EnableColor;
    [SerializeField] private Color costumeFilter_DisableColor;
    [SerializeField] private List<UIOutline> costumeFilter_All;
    [SerializeField] private List<UIOutline> costumeFilter_Body;
    [SerializeField] private List<UIOutline> costumeFilter_Acce;


    [Header(" - ClosetGroup")]
    [SerializeField] private GameObject emptyObj;
    [SerializeField] private List<UIOutline> closetFilter_Costume;
    [SerializeField] private List<UIOutline> closetFilter_BG;

    enum MainTab
    {
        Costume,
        Bg,
        Closet,
        Album,
    }

    enum CostumeTab
    {
        All,
        Body,
        Accessory
    }

    enum ClosetTab
    {
        Costume,
        Background
    }

    MainTab tab = MainTab.Costume;
    CostumeTab cosFilter = CostumeTab.All;
    ClosetTab closetFilter = ClosetTab.Costume;

    // 배경용 이미지 프리펩 딕셔너리
    private Dictionary<string, GameObject> bgObjDic = new Dictionary<string, GameObject>();
    private GameObject currentBgObj = null;
    int crrent_price = 0;

    public override void _Start()
    {
        base._Start();
    }

    public void Bind()
    {
        tab = MainTab.Costume;
        cosFilter = CostumeTab.All;
        lobbyUI.m_PengsooController.Init(true);
        PengsooVisibleManager.Instance.PushPengsooModel(lobbyUI);
        CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);
    }

    public override void OnOpen()
    {
        base.OnOpen();

        GameData.costumeinfo.ClearAll_Preview_ItemData();
        GameData.costumeinfo.Clear_Cart();

        costumeScroll.SetSelectedCellCallback((data =>
        {
            CostumeScrollCellData item = data as CostumeScrollCellData;

            costumeScroll.UpdateAll();

            UpdatePengsoo();
            UpdateBuyBtn();
        }));

        bgScroll.SetSelectedCellCallback((data =>
        {
            CostumeScrollCellData item = data as CostumeScrollCellData;

            bgScroll.UpdateAll();

            UpdateBG();
            UpdateBuyBtn();
        }));

        closetScroll.SetSelectedCellCallback((data =>
        {
            ClosetScrollCellData item = data as ClosetScrollCellData;

            closetScroll.UpdateAll();

            UpdateBG(false);
            UpdatePengsoo(false);
        }));

        albumScroll.SetSelectedCellCallback((data =>
        {
            CostumeScrollAlbumCellData item = data as CostumeScrollAlbumCellData;
            if (item.IsCaputureBtn)
            {
                PopupManager.Instance.OpenPopup<CostumeCapturePopupController>(onBindAction: popup =>
                    {
                        popup.Bind(false);
                    },
                    onCloseAction: () =>
                    {
                        UpdateUI(false);
                        Refresh_AlbumScroll();
                    });

                return;
            }
            else
            {
                PopupManager.Instance.OpenPopup<CostumeCaptureViewPopupController>(
                    onBindAction: popup =>
                    {
                        CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.FRONT);
                        popup.Bind(true, item, albumScroll);
                    },
                    onCloseAction: () =>
                    {
                        CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);
                        UpdateUI(false);
                    });
            }

        }));
    }

    public override void OnOpened()
    {
        base.OnOpened();
        mainToggleGroup_Default.isOn = true;
        OnMainTab1(true);
    }


    public override void OnClosePopup()
    {
        base.OnClosePopup();
        CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.FRONT);
        PengsooVisibleManager.Instance.PopPengsooModel();
    }

    public void OnMainTab1(bool tf)
    {
        tab = MainTab.Costume;

        costumeToggleGroup_Default.isOn = true;
        SmapleBtn.SetActive(true);
        ResetBtn.SetActive(true);

        UpdateUI();
        OnCosFilterAll(true);
        UpdateBuyBtn();
    }

    public void OnMainTab2(bool tf)
    {
        tab = MainTab.Bg;

        SmapleBtn.SetActive(true);
        ResetBtn.SetActive(true);

        UpdateUI();
        Refresh_BGScroll();
        UpdateBuyBtn();
    }
    public void OnMainTab3(bool tf)
    {
        tab = MainTab.Closet;

        closetToggleGroup_Default.isOn = true;
        SmapleBtn.SetActive(false);
        ResetBtn.SetActive(false);

        UpdateUI(false);
        OnClosetFilterCostume(true);
    }

    public void OnMainTab4(bool tf)
    {
        tab = MainTab.Album;

        SmapleBtn.SetActive(false);
        ResetBtn.SetActive(false);

        UpdateUI(false);
        Refresh_AlbumScroll();

    }

    public void OnShowCartBtn()
    {
        PopupManager.Instance.OpenPopup<CostumeCartPopupController>(onBindAction: popup =>
            {
                CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.FRONT);
                popup.Bind();
            },
            onCloseAction: () =>
            {
                CameraManager.Instance.ChangeCameraDepthByEnum("Popup", CameraManager.CamDepth.MIDDLE);
                UpdateCart();
                UpdateUI();
                switch (tab)
                {
                    case MainTab.Costume:
                        {
                            costumeToggleGroup_Default.isOn = true;
                            OnCosFilterAll(true);
                        }
                        break;
                    case MainTab.Bg:
                        {
                            Refresh_BGScroll();
                        }
                        break;
                    default:
                        break;
                }
            });
    }

    public void OnShowSampleBtn()
    {
        PopupManager.Instance.OpenPopup<CostumeCapturePopupController>(onBindAction: popup =>
        {
            popup.Bind(true);
        },
        onCloseAction: () =>
        {
        });
    }
    public void OnResetCartBtn()
    {
        GameData.costumeinfo.Clear_Cart();
        GameData.costumeinfo.ClearAll_Preview_ItemData();

        UpdateCart();
    }

    private void UpdateCart()
    {
        UpdateBG();
        UpdatePengsoo();

        costumeScroll.UpdateAll();
        bgScroll.UpdateAll();
        UpdateBuyBtn();
    }

    void UpdateUI(bool isPreview = true)
    {
        costumeGroup.gameObject.SetActive(tab == MainTab.Costume);
        bgGroup.gameObject.SetActive(tab == MainTab.Bg);
        closetGroup.gameObject.SetActive(tab == MainTab.Closet);
        albumGroup.gameObject.SetActive(tab == MainTab.Album);

        UpdateBG(isPreview);
        UpdatePengsoo(isPreview);
    }

    public void UpdateBG(bool isPreview = true)
    {
        if (currentBgObj != null)
            currentBgObj.SetActive(false);

        GameData.Costume_ItemData itemData = LoadBG(isPreview);

        if (itemData == null)
        {
            currentBgObj?.SetActive(false);
            return;
        }

        string code;
        code = itemData?.itemCd;

        if (string.IsNullOrEmpty(code))
        {
            currentBgObj.SetActive(false);
            return;
        }

        if (!bgObjDic.ContainsKey(code))
        {
            GameObject bgObj = AssetBundleManager.Instance.Load_LobbyBG_FromCode(code + "_costumepopup_bg", 
                                                                                 "Assets/Bundles/Lobby_BG/costumepopup_bg/", 
                                                                                 BG_Root.transform);
            bgObjDic[code] = bgObj;
        }

        currentBgObj = bgObjDic[code];
        currentBgObj.SetActive(true);
    }

    public GameData.Costume_ItemData LoadBG(bool isPreview)
    {
        if (isPreview)
            return GameData.costumeinfo.GetPreview_ItemData(GameData.CostumeCategory.BG);
        else
            return GameData.costumeinfo.GetEquipped_ItemData(GameData.CostumeCategory.BG);
    }

    public void UpdatePengsoo(bool isPreview = true)
    {
        lobbyUI.m_PengsooController.UpdatePengsoo(isPreview);
        lobbyUI.PlayPengsoo_Special();
    }

    public void UpdateBuyBtn()
    {

        if (GameData.costumeinfo.IsEmpty_Cart())
        {
            Buy_Button.SetActive(false);
            return;
        }

        Buy_Button.SetActive(true);

        Buy_Price.text = GameData.costumeinfo.GetPrice_Cart().ToString();
    }

    #region Costume_Gruoup
    public void OnCosFilterAll(bool tf)
    {
        if (tf == true)
        {
            cosFilter = CostumeTab.All;

            ChangeOutline(costumeFilter_All, costumeFilter_EnableColor);
            ChangeOutline(costumeFilter_Body, costumeFilter_DisableColor);
            ChangeOutline(costumeFilter_Acce, costumeFilter_DisableColor);

            Refresh_CostumScroll();
        }
        else
        {
        }
    }

    public void OnCosFilterBody(bool tf)
    {
        if (tf == true)
        {
            cosFilter = CostumeTab.Body;

            ChangeOutline(costumeFilter_All, costumeFilter_DisableColor);
            ChangeOutline(costumeFilter_Body, costumeFilter_EnableColor);
            ChangeOutline(costumeFilter_Acce, costumeFilter_DisableColor);

            Refresh_CostumScroll();

        }
        else
        {
        }
    }
    public async void OnCosFilterAcce(bool tf)
    {
        if (tf == true)
        {
            cosFilter = CostumeTab.Accessory;

            ChangeOutline(costumeFilter_All, costumeFilter_DisableColor);
            ChangeOutline(costumeFilter_Body, costumeFilter_DisableColor);
            ChangeOutline(costumeFilter_Acce, costumeFilter_EnableColor);

            Refresh_CostumScroll();
        }
        else
        {
        }
    }

    public void Refresh_CostumScroll()
    {

        string formatString = "yyyyMMddHHmm";
        //TimeSpan newdays = new TimeSpan(30, 0, 0, 0);
        TimeSpan newdays = new TimeSpan(7, 0, 0, 0);        // HGKIM 220725 - 7일동안 표기해달라 해서 변경
        GameData.CostumeCategory scrollCategory = GameData.CostumeCategory.BODY | GameData.CostumeCategory.ACC_FACE | GameData.CostumeCategory.ACC_HEAD;

        costumeScroll.ClearList();
        foreach (KeyValuePair<string, GameData.Costume_ProductData> product in GameData.costumeinfo.ProductList)
        {
            GameData.Costume_ProductData data = product.Value;
            if (null == data)
                continue;

            if (!scrollCategory.HasFlag(data.category))
                continue;

            DateTime sale_EndDate = DateTime.ParseExact(data.productData.endDate, formatString, null);
            if (sale_EndDate < DateTime.Now)
                continue;

            DateTime sale_StartDate = DateTime.ParseExact(data.productData.startDate, formatString, null);
            GameData.CostumeCategory acceCategory = GameData.CostumeCategory.ACC_FACE | GameData.CostumeCategory.ACC_HEAD;
            switch (cosFilter)
            {
                case CostumeTab.All:
                    break;
                case CostumeTab.Body:
                    {
                        if (data.category != GameData.CostumeCategory.BODY)
                            continue;
                    }
                    break;
                case CostumeTab.Accessory:
                    {
                        if (!acceCategory.HasFlag(data.category))
                            continue;
                    }
                    break;
                default:
                    break;
            }

            costumeScroll.AddCell(new CostumeScrollCellData
            {
                productCode = data.productData.productCode,
                categoryCode = data.productData.productCode.Substring(1, 2),
                price = data.productData.price,
                discountRate = data.productData.discountRate,
                isSelected = false,
                isOwned = data.IsOwned,
                isSeason = (SPEAK_SaleType)data.productData.saleType == SPEAK_SaleType.SEASON_LIMIT,
                isNew = data.productData.isNew,//((DateTime.Now - sale_StartDate) < newdays) ? true : false,
                isHot = data.productData.isHot,
            });

            // HGKIM 220808 - 아이템이 없는지 체크
            if(CheckedCostumeIMGisNULL(data.productData.productCode))
			{
                break;
			}
        }

        costumeScroll.InitEndScroll();
    }

    private bool CheckedCostumeIMGisNULL(string productCode)
	{

        // HGKIM 220808 - 아이템이 없을 경우 앱 다시 시작하기 - 220810 종료로 변경
        if (AssetBundleManager.Instance.Load_CostumeProduct_CostumeIMG(productCode) == null)
        {
            Logger.Log($"HGKIM || CustumeScrollCell.cs || 해당 데이터가 없음!!!! >> {productCode}");
            PopupManager.Instance.OpenPopup<CommonPopupController>(popup =>
            {
                popup.SetSystemPopupController("앱 업데이트가 있습니다. 패치를 받기 위해 앱을 종료합니다.", okAction: () => {
                    //SceneController.Instance.SceneLoad("Intro", () => { });
                    //SceneController.Instance.BackToIntroFromLobby();  // 로그아웃 루틴에 인트로로 되돌리는 함수가 있어서 넣어봄
                    Application.Quit();
                });
            });

            return true; 
        }
        // HGKIM 220808 - 아이템이 없을 경우 앱 다시 시작하기 여기까지

        return false;
	}

    private void ChangeOutline(List<UIOutline> outlines, Color coloer)
    {
        foreach (var outline in outlines)
        {
            outline.effectColor = coloer;
        }
    }
    #endregion Costume_Gruoup

    #region BG_Gruoup
    public void Refresh_BGScroll()
    {
        string formatString = "yyyyMMddHHmm";
        //TimeSpan newdays = new TimeSpan(30, 0, 0, 0);
        TimeSpan newdays = new TimeSpan(7, 0, 0, 0);        // HGKIM 220725 - 7일동안 표기해달라 해서 변경

        bgScroll.ClearList();
        foreach (KeyValuePair<string, GameData.Costume_ProductData> product in GameData.costumeinfo.ProductList)
        {
            GameData.Costume_ProductData data = product.Value;
            if (null == data)
                continue;


            if (data.category != GameData.CostumeCategory.BG)
                continue;

            DateTime sale_EndDate = DateTime.ParseExact(data.productData.endDate, formatString, null);
            if (sale_EndDate < DateTime.Now)
                continue;

            DateTime sale_StartDate = DateTime.ParseExact(data.productData.startDate, formatString, null);

            bgScroll.AddCell(new CostumeScrollCellData
            {
                productCode = data.productData.productCode,
                categoryCode = data.productData.productCode.Substring(1, 2),
                price = data.productData.price,
                discountRate = data.productData.discountRate,
                isSelected = false,
                isOwned = data.IsOwned,
                isSeason = (SPEAK_SaleType)data.productData.saleType == SPEAK_SaleType.SEASON_LIMIT,
                isNew = data.productData.isNew,//((DateTime.Now - sale_StartDate) < newdays) ? true : false,
                isHot = data.productData.isHot,
            });
        }

        bgScroll.InitEndScroll();
    }
    #endregion BG_Gruoup

    #region Closet_Group
    public async void OnClosetFilterCostume(bool tf)
    {
        if (tf == true)
        {
            closetFilter = ClosetTab.Costume;

            ChangeOutline(closetFilter_Costume, costumeFilter_EnableColor);
            ChangeOutline(closetFilter_BG, costumeFilter_DisableColor);

            Refresh_ClosetScroll();

        }
        else
        {
        }
    }
    public async void OnClosetFilterBG(bool tf)
    {
        if (tf == true)
        {
            closetFilter = ClosetTab.Background;

            ChangeOutline(closetFilter_Costume, costumeFilter_DisableColor);
            ChangeOutline(closetFilter_BG, costumeFilter_EnableColor);

            Refresh_ClosetScroll();
        }
        else
        {
        }
    }

    public void Refresh_ClosetScroll()
    {
        closetScroll.ClearList();

        foreach (GameData.Costume_ItemData item in GameData.costumeinfo.OwnedList)
        {
            switch (closetFilter)
            {
                case ClosetTab.Costume:
                    {
                        if (item.Category() == GameData.CostumeCategory.BG)
                            continue;
                    }
                    break;
                case ClosetTab.Background:
                    {
                        if (item.Category() != GameData.CostumeCategory.BG)
                            continue;
                    }
                    break;
                default:
                    break;
            }

            closetScroll.AddCell(new ClosetScrollCellData
            {
                itemCode = item.itemCd,
                category = item.Category(),
                isSeason = (GameData.costumeinfo.IsSeason(item.itemCd)) ? true : false,
                isSelected = false
            }); ;
        }

        if (closetScroll.GetItemCount() > 0)
            emptyObj.SetActive(false);
        else
            emptyObj.SetActive(true);

        closetScroll.InitEndScroll();
    }

    #endregion Closet_Group

    #region Album_Group

    #endregion Album_Group
    string[] files;
    public void Refresh_AlbumScroll()
    {
        albumScroll.ClearList();

        albumScroll.AddCell(new CostumeScrollAlbumCellData
        {
            filePath = string.Empty,
        });

        // 230508 - 폴더가 없을 경우에 대한 예외처리
        if(Directory.Exists(GameData.imageCapture.SavePath).Equals(false))
		{
            Directory.CreateDirectory(GameData.imageCapture.SavePath);
		}
        files = Directory.GetFiles(GameData.imageCapture.SavePath, "*.png");
        files = files.OrderBy(x => x).ToArray();
        ///[2021-운영-3-QA/22]
        ///위 이슈 수정을 위한 임시조치.
        foreach (string file in files.Take(15))
        {
            albumScroll.AddCell(new CostumeScrollAlbumCellData
            {
                filePath = file,
            });
        }
        albumScroll.InitEndScroll();
        albumScroll.AddOverEndScrollEvent(OnOverEndScrollEvent_AlbumScroll);
    }

    public void OnOverEndScrollEvent_AlbumScroll(eBigScrollDirection direction)
    {
        albumScroll.RemoveOverEndScrollEvent();
        if (direction == eBigScrollDirection.Top)
        {
        }
        else if (direction == eBigScrollDirection.Bottom)
        {
            if (files.Count() == albumScroll.GetCellItemCount() - 1)
                return;

            DoLoadNext();
        }

    }

    private void DoLoadNext()
    {
        foreach (string file in files.Skip(albumScroll.GetCellItemCount() - 1).Take(15))
        {
            albumScroll.AddCell(new CostumeScrollAlbumCellData
            {
                filePath = file,
            });
        }
        albumScroll.AddOverEndScrollEvent(OnOverEndScrollEvent_AlbumScroll);
    }
}

