% fit Lqi Prr data by sigmf function
% Zhen Song
% 7/24/2006
clear all;
jul20withdistdat_std;
lqi=LqiPrr(1,:);
prr=LqiPrr(2,:);
global lqi 
global prr
c0=[0.2; 0];
[c, resnorm, residual] = lsqcurvefit(@CostFunSigMF,c0,lqi,prr);
t=linspace(40,140);
prrfit=1./(1+exp(-c(2)*(t-c(1))));

A=[lqi' ones(size(lqi'))];
b=prr';
k=pinv(A)*b;
linres= sum((A*k-b).^2 );

figure;
hold on;
for cnt=1:max(LqiPrr(3,:))
    ind=find(LqiPrr(3,:)==cnt);
    %    plot(LqiPrr(1,ind),LqiPrr(2,ind),'r');
    h1=plot( mean(LqiPrr(1,ind)), LqiPrr(2,ind(1)),'dr');
end
h2=plot(t, prrfit);
prrlinfit=[t' ones(size(t'))]*k;
h3=plot(t, prrlinfit','-.');
legend([h1, h2, h3],'Real','Curve Fit','Line Fit');
xlabel('LQI'); ylabel('Packet Reception Rate (PRR)');
grid;
%legend([h1,h2],'samples','mean');
% axis([50 130 0 1]);
title('LQI/PRR');

disp(['Error for curve fitting=', num2str(resnorm), '. Error for line fitting=', num2str(linres)]);