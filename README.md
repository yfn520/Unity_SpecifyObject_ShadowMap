自制高精阴影
原理：
单独渲染物件到一张shadowmap，运行时采样该独立的shadowmap，独立shadowmap是好处就是可以做镜头特写，并且使用的阴影贴图分辨率可以很低。

运行环境：URP7.7-2019.4.36。
一、	自定义一个Lit.shader叫Custom_Lit_Shadow把相关代码改在里面，如Bench Bottom
需要在renderFeature中添加渲染阴影的Feature即可
 ![image](https://github.com/yfn520/Unity_SpecifyObject_ShadowMap/assets/16619612/d00a99dd-3e06-4696-a4eb-44139a61e8c5)

二、	不修改物件的材质，利用贴花的方式渲染阴影，如地面，需要开启depthTexture。
需要额外添加两个pass：
 ![image](https://github.com/yfn520/Unity_SpecifyObject_ShadowMap/assets/16619612/5ebebc79-ada7-4f27-bec2-f958fd9f61bd)

Mask是用于去除自投影，如果自身没有投影，想全部用该高精阴影，可以去除该pass。
三、	目前仅运行时才会生效，CustomShadowMgr里控制需要做独立阴影的物件，调用AddShadowCaster的接口添加，使用时可以直接把该脚本挂在需要特写的物件身上例子中的Workbench，运行后即可看到阴影：
![image](https://github.com/yfn520/Unity_SpecifyObject_ShadowMap/assets/16619612/30303ef4-4359-4f76-b876-6fadc4a291de)
 

