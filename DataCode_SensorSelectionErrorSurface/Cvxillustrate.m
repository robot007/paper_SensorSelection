% contour
close all;
vx=[0 0 0;... 
    1 0 1;...
    1 0 1;...
    0 0 0]*2;
vy=[0 0 0;...
    0 1 0;...
    1 1 0;...
    1 0 0]*2;
vz=[0 0 0;...
    0 0 0;...
    0 1 1;...
    0 1 1]*2;
hf=patch(vx,vy,vz,'y');
set(hf, 'FaceAlpha',0.5);
hold on;
% alpha(0.3);
xlabel('p_1'); ylabel('p_2'); zlabel('p_3')
h=text(1.9,0,0,'p_1'); set(h,'FontSize',20);
h=text(0, 1.9,0,'p_2'); set(h,'FontSize',20);
h=text(0, 0, 1.9,'p_3'); set(h,'FontSize',20);
grid on;

p0=[-1; 1];
xmin=-2; xmax=2.5;
ymin=-1; ymax=3.5;

hslice = surf(linspace(xmin,xmax,100),...
linspace(ymin,ymax,100),...
ones(100));

rotate(hslice,[-45, 0],-45,[0,0,1])
xd = get(hslice,'XData');
yd = get(hslice,'YData');
zd = get(hslice,'ZData');
delete(hslice)

[x,y,z] = meshgrid(xmin:.2:xmax, ymin:0.2:ymax, -0.5:.2:3);
v = ((x-p0(1)*ones(size(x))).^2 + (y-p0(2)*ones(size(y))).^2).^(0.5);

h = slice(x,y,z,v,xd,yd,zd);

set(h,'FaceColor','interp','EdgeColor','none','DiffuseStrength',.8);
[x2,y2] = meshgrid(xmin:.2:xmax, ymin:0.2:ymax);
val2=((x2-p0(1)*ones(size(x2))).^2 + (y2-p0(2)*ones(size(y2))).^2).^(0.5);
% h3=contour(x2,y2,val2);

%  h2 = contourslice(x,y,z,v,xd,yd,zd);
camproj perspective;
campos([20,40,10]);
axis equal;

