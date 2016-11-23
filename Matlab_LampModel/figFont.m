function figFont(fignum)
figure(fignum);
% enlarge
set(get(gca,'XLabel'),'FontSize',14);
set(get(gca,'YLabel'),'FontSize',14);
set(get(gca,'ZLabel'),'FontSize',14);
set(gca,'FontSize',14);