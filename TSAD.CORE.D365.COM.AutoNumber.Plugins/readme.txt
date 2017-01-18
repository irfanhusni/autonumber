-- REPACK REDUCE
- Change InputAssemblies 
- <RepackReduce InputFileName="$(TargetPath)" KeyFile="@(KeyFile)" EntityTypes="TSAD.CORE.D365.Entities.*" />
	- change EntityTypes namespace

  <Target Name="AfterBuild">
    <ItemGroup>
      <InputAssemblies Include="$(TargetPath)" />
      <InputAssemblies Include="$(TargetDir)\TSAD.CORE.D365.COM.ManualNumber.dll" />
      <InputAssemblies Include="$(TargetDir)\TSAD.CORE.D365.Entities.dll" />
      <InputAssemblies Include="$(TargetDir)\TSAD.XRM.Framework.Auto365.dll" />
      <InputAssemblies Include="$(TargetDir)\TSAD.XRM.Framework.dll" />
    </ItemGroup>
    <ItemGroup>
      <DllMerge Include="$(SolutionDir)packages\ILRepack.2.0.12\tools\ILRepack.exe" />
      <KeyFile Include="$(SolutionDir)TSAD.D365.snk" />
    </ItemGroup>
    <Exec Command="@(DllMerge) /keyfile:@(KeyFile) /parallel /internalize /out:$(TargetPath) /lib:$(TargetDir) @(InputAssemblies -> '%(Identity)', ' ')" />
    <RepackReduce InputFileName="$(TargetPath)" KeyFile="@(KeyFile)" EntityTypes="TSAD.CORE.D365.Entities.*" ExcludeTypes="TSAD.XRM.Framework.*" />
  </Target>