   M S B A M L   `   `   `  ����N  JBuggy File Transfer, Version=2.4.8.0, Culture=neutral, PublicKeyToken=nullK*clr-namespace:FileSharingApp_Desktop.PagesFileSharingApp_Desktop.Pages  5      U/clr-namespace:FileSharingApp_Desktop.Properties!FileSharingApp_Desktop.Properties  5      3    -FileSharingApp_Desktop.Pages.TransferDonePage   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      <local*clr-namespace:FileSharingApp_Desktop.Pages     5      =p/clr-namespace:FileSharingApp_Desktop.Properties     5      -   5          N� Title$  TransferDonePage��5
      .��5      � ���#353536$��5,5,5,5q�6     � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $��4* �6   �� 5      $��* �6   .+�5   
   �� $��BC_logo_1.png��6   $��3��6'   $��3��68    J� Opacity$	 0.1Q�6M   �� 5   
   $��0��6   $��0,0,0,2q�6   $
��10,0q�5      
����ح�6   �� 5   
   $��1��6   $��0,0,0,2q�6   $
��10,0q�5      
����ح�6   �� 5   
    �� Orientation$ 
Horizontal=�6   $��Center=�6.   .+�5       �� $��/Icons/check.png��6   $��0,0,30,0q�5!      $��50��5"      $��50��6   �� 5$      1   +FileSharingApp_Desktop.Properties.Resources   Done_InfoMessage#��Z 6   $��25(�5%      
�������$��Center=�5&      $��Center=�6+   $��0��5'      5)      �� 5+   
   -   5         ��Name$ 
list_Files��5+      $��1��5,      $��Center=�6    $��Center=�5-      
�������$��10q�5.      $	��700��6   $	��300��6+   � 50   
   $��4��6    � ColumnDefinitions 51      �� 52      $��5* �6#   �� 53      $��* �6#   �� 54      $��5* �6#   �� 55      $��* �6#   �� 56      $��5* �6#   .+�58      �� -   5         ��Name$ btn_MainMenu��58      $��0��6+      Blue#��[  59      $��10q�63   $��20(�5;      
�������.��5=      �� $ 
Horizontal=�6   $	��180��66   .+�5>      �� $��/Icons/Home.png��6   $	�� 40��5?       $��40��6-   $
��Left=�5@       $	��5,0q�6<   �� 5A         Done_btnHome#��Z 6    $��WrapWithOverflow=�6K   $	��5,0q�5B   !   $��Center=�5C   !   $��Right=�6<   5D      5E      �� 5G      -   5        $ btn_OpenFile��5G      $��2��6+   #��[  5H      $��10q�63   $��20(�5J      
�������.��5L      �� $ 
Horizontal=�6   $	��180��66   .+�5M      �� $��/Icons/menu.png��6   $	�� 60��5N       $��40��6-   $
��Left=�5O       $	��5,0q�6<   �� 5P      	   Done_btnOpen#��Z	 6    $��WrapWithOverflow=�6K   $	��5,0q�5Q   !   $��Center=�5R   !   $��Right=�6<   
 �� TextAlignment$
 Center=�5S        �� Padding$ 2q�67   5T      5U      �� 5V      -   5        $ btn_OpenFolder��5V      $��4��6-   #��[  5W      $��10q�63   $��20(�5Y      
�������.��5Z      �� $ 
Horizontal=�6   $	��200��66   .+�5[      �� $��/Icons/folder.png��6   $	�� 40��5\       $��40��6-   $
��Left=�5]       $	��5,0q�6<   �� 5^         Done_OpenFolder#��Z 6    $��WrapWithOverflow=�6N   $	��5,0q�5_   !   $	��150��6.   $��Center=�5`   !   $��Right=�6<   $
 Center=�5a       $ 2q�67   5b      5c      5d      5e      5f      